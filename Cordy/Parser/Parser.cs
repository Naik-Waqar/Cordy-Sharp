using Cordy.AST;
using Cordy.Codegen;
using Cordy.Exceptions;
using System;
using System.Collections.Generic;


namespace Cordy
{
    using static eLexemType;
    //TODO: Comment all this stuff
    public class Parser : CompilerPart
    {
        #region "Bridge"

        private Lexer Lexer;
        //private Listener Listener;
        private CordyType Type;
        private Generator Generator;

        private Lexem Current => Lexer.Current;

        #endregion

        #region Constructors

        internal Parser(CordyType type, Lexer lexer, Generator generator)
        {
            Type = type;
            Lexer = lexer;
            Generator = generator;
        }

        public Parser(Lexer lexer) => Lexer = lexer;

        #endregion

        #region Consumables

        private TypeNode handledType;
        private eAccessLevel lvl;
        private bool isProtected;
        private bool isStatic;
        private bool isSealed;
        private eTypeContext context;

        private List<string> Parameters { get; } = new List<string>();
        private List<string> Attributes { get; } = new List<string>();

        #endregion

        #region DebugInfo

        public override string Stage { get; } = "Parser";
        public override string FileName => Type.FullName;
        public override (int, int)? Pos => Current.Pos;

        #endregion

        #region Driver

        /// <summary>
        /// Parsing in global context
        /// </summary>
        internal void Parse()
        {
            ClearConsumables();

            //TODO: Use exceptions for error handling
            //TODO: Move switch to separate method
            try
            {
                while (true)
                {
                    try
                    {
                        GenerateElement();
                    }
                    catch (exUnexpected e)
                    {
                        Error(e.Message);
                        Clear();
                    }
                    catch (exBadDefinition e)
                    {
                        Error(e.Message);
                        Clear();
                    }
                    catch (exBadDeclarationPos e)
                    {
                        Error(e.Message);
                        Clear();
                    }
                    catch (NotImplementedException e)
                    {
                        Error(e.Message);
                        Clear();
                    }
                }
            }
            catch (exEOF)
            {
                return;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        private void GenerateElement()
        {
            switch (Lexer.Next().Type)
            {
                case EOF:
                    throw new exEOF();

                //ignore trash
                case NewLine:
                case Indent:
                case PreprocessorDirective:
                case MultiLineComment:
                case SingleLineComment:
                    return;

                #region Include

                case Key_Include:
                    Lexer.Next();
                    if (Current.Type != String)
                    {
                        Error("'include' operator requires a string as it's argument");
                        Lexer.SkipToEOL();
                        return;
                    }
                    return;

                #endregion

                #region Parameter
                // ^{Parameter}^
                // ^{Parameter}
                //  {Parameter}
                case CurlyBracketOpen:
                    ParseParameter();
                    return;
                #endregion

                #region Attribute
                // ^[Attribute]^
                // ^[Attribute]
                //  [Attribute]
                case SquareBracketOpen:
                    ParseAttribute();
                    return;
                #endregion

                #region Access Modifiers

                //Access Level
                case Key_AccessLevel:
                    if (lvl == eAccessLevel.Undefined)
                    {
                        lvl = (eAccessLevel)Enum.Parse(typeof(eAccessLevel), Current.Value, true);
                        return;
                    }

                    Info($"Definition already have 'access level' modifier. Excess one ignored");
                    return;

                //Protected
                case Key_Protected:
                    if (isProtected)
                        Info($"Definition already have 'protected' modifier. Excess one ignored");

                    isProtected = true;
                    return;

                //Static
                case Key_Static:
                    if (isStatic)
                        Info($"Definition already have 'static' modifier. Excess one ignored");

                    isStatic = true;
                    return;

                //Sealed
                case Key_Sealed:
                    if (signatureParsed)
                    {
                        Warn($"'sealed ' modifier can't be applied to definition. Ignored");
                        return;
                    }

                    if (isSealed)
                        Info($"Type signature already have 'sealed' modifier. Excess one ignored");

                    isSealed = true;
                    return;

                #endregion

                #region File context

                //File Context
                case Key_FileContext:
                    if (signatureParsed)
                        throw new exTooManySignatures();

                    //TODO: Add parents for type
                    //TODO: Add generic pattern for type
                    //TODO: Add settings for type
                    context = (eTypeContext)Enum.Parse(typeof(eTypeContext), Current.Value, true);
                    signatureParsed = true;
                    Lexer.SkipToEmptyLine();
                    ClearConsumables();
                    return;

                #endregion

                #region Event

                // Handle event
                case Key_Event:
                    if (!signatureParsed || context == eTypeContext.Enum)
                        throw new exBadDeclarationPos("Event");

                    Handle("Event");
                    return;

                #endregion

                #region Constructor

                // Handle constructor
                case Key_New:
                    if (!signatureParsed || context == eTypeContext.Enum)
                        throw new exBadDeclarationPos("Constructor");

                    Handle("Constructor");
                    return;

                #endregion

                #region Indexer

                // Handle indexer
                case Key_This:
                    if (!signatureParsed || context == eTypeContext.Enum)
                        throw new exBadDeclarationPos("Indexer");

                    Handle("Indexer");
                    return;

                #endregion

                //Function, property or operator
                case Identifier:
                    handle:
                    switch (Lexer.Next().Type)
                    {
                        case Indent: //TODO: Make type recognition more safe
                        case MultiLineComment:
                        case SingleLineComment:
                        case PreprocessorDirective:
                            goto handle;

                        #region Property
                        //             id    '\n'
                        //             id    'is'
                        //             id    '='
                        //Property     ^name  ^checking
                        case NewLine: //TODO: Make NL skips
                        case Key_Is:
                        case Op_Assignment:
                            if (!signatureParsed || context == eTypeContext.Enum)
                                throw new exBadDeclarationPos("Property");

                            Handle("Property");
                            return;
                        #endregion

                        #region Function
                        //              id      '('
                        // Functiion    ^ name   ^checking
                        case RoundBracketOpen:
                            if (!signatureParsed || context == eTypeContext.Enum)
                                throw new exBadDeclarationPos("Function");

                            Handle("Function");
                            return;
                        #endregion

                        #region Type or Name
                        // id     '{'
                        // id     ':'
                        // id     id
                        // ^type   ^checking
                        // we got the type. If now is id -> use prev as type and current will be as name
                        case CurlyBracketOpen:
                        case Operator when Current.Value == ":":
                        case Identifier:
                            if (handledType != null)
                                throw new exBadDefinition($"have more than one type ('{handledType.ToString()}' and '{Current.Value}')");

                            handledType = ParseType();
                            if (handledType == null)
                                throw new exBadDefinition("Only operators, indexers, and constructors can be without name");
                            break;
                        #endregion

                        #region Operator
                        //Handle operator definition
                        case Operator:
                            if (!signatureParsed || context == eTypeContext.Enum)
                                throw new exBadDeclarationPos("Operator");

                            Handle("Operator");
                            return;
                        #endregion

                        default:
                            throw new exUnexpected(Current.Value);
                    }

                    // we are here because handled the type
                    // if now is id -> checking for next with same rules
                    // if not -> error
                    if (Current.Type == Identifier)
                        goto handle;
                    return;

            }
        }

        #endregion

        #region Helper

        private bool signatureParsed;

        private void Clear()
        {
            ClearConsumables();
            Lexer.SkipToEmptyLine();
        }

        private string ToStringBetween(string start, string end)
        {
            var c = 1;
            var s = "";
            if (Current.Value == start)
                Lexer.Next();

            while (c > 0 && Current.Type != EOF)
            {
                if (Current.Value == start)
                    c++;
                else if (Current.Value == end)
                    c--;

                if (c > 0)
                    s += Current.Value;

                Lexer.Next();
            }
            return s;
        }

        private void ClearConsumables()
        {
            isStatic = isProtected = isSealed = false;
            lvl = eAccessLevel.Undefined;
            Parameters.Clear();
            Attributes.Clear();
        }

        private static TypeNode Void { get; } = new TypeNode("void", null, null);

        #endregion

        #region Handling

        public void Handle(string member)
        {
            //Listener.EnterRule($"Handle{member}Definition");
            var m = (DefinedNode)typeof(Parser).GetMethod("Parse" + member).Invoke(this, null);
            m.Definition.ApplyParameters(Parameters);
            //Listener.ExitRule(m);
            if (m == null)
                throw new exBadDefinition(member);
            Generator.Emit(m);
            //Listener.Listen();
            ClearConsumables();
        }

        #endregion

        #region Elementary parsing

        #region Modifiers

        private void ParseAttribute()
        {
            if (Lexer.Prev.Value == "^")
            {
                Type.ApplyAttribute(ToStringBetween("[", "]"));
                if (Current.Value == "^")
                    Lexer.Next();
                return;
            }
            Attributes.Add(ToStringBetween("[", "]"));
        }

        private void ParseParameter()
        {
            if (Lexer.Prev.Value == "^")
            {
                Type.ApplyParameter(ToStringBetween("{", "}"));
                if (Current.Value == "^")
                    Lexer.Next();
                return;
            }
            Parameters.Add(ToStringBetween("{", "}"));
        }

        #endregion

        #region Type Members

        public Indexer ParseIndexer() => throw new NotImplementedException();

        public Constructor ParseConstructor() => throw new NotImplementedException();

        public Event ParseEvent() => throw new NotImplementedException();

        public Property ParseProperty() => throw new NotImplementedException();

        public Operator ParseOperator()
        {
            var code = Current.Value;
            Lexer.Next();
            var args = ParseFunctionArgs(RoundBracketOpen, RoundBracketClose);
            var body = ParseBlock();
            var def = new OperatorDef(handledType ?? Void, args, code);
            return new Operator(def, body);
        }

        public Function ParseFunction()
        {
            var name = Lexer.Prev.Value;
            var args = ParseFunctionArgs(RoundBracketOpen, RoundBracketClose);
            var body = ParseBlock();
            var def = new FunctionDef(lvl, isProtected, isStatic, handledType, name, args);
            return new Function(def, body);
        }

        #endregion

        #region Type

        public TypeNode ParseType()
        {
            var r = 0;
            return ParseType(ref r);
        }

        public TypeNode ParseType(ref int r)
        {
            var name = Lexer.Prev.Value;
            var settings = new List<ExprNode>();
            var template = new List<TypeNode>();
            check:
            switch (Current.Type)
            {
                case Operator when Current.Value == ":":
                    Lexer.Next();
                    settings.Add(ParsePrimary());
                    goto check;
                case CurlyBracketOpen:
                    Lexer.Next();
                    r++;
                    template.AddRange(ParseTypeSequence(ref r));
                    goto check;

                case Indent:
                case NewLine:
                case MultiLineComment:
                case SingleLineComment:
                case PreprocessorDirective:
                    Lexer.Next();
                    goto check;

                case Identifier:
                case Operator:
                case CurlyBracketClose:
                    return new TypeNode(name, settings, template);
                default:
                    Error($"Unexpected token '{Current.Value}'");
                    return null;
            }
        }

        public List<TypeNode> ParseTypeSequence(ref int r)
        {
            var o = new List<TypeNode>();
            while (r > 0)
            {
                switch (Current.Type)
                {
                    case CurlyBracketClose:
                        r--;
                        Lexer.Next();
                        return o;
                    case Operator when Current.Value == ",":
                        Lexer.Next();
                        break;
                }
                Lexer.Next();
                o.Add(ParseType(ref r));
            }
            return o;
        }

        #endregion

        #region Expressions

        #region Operator Expressions

        //TODO: Make parsing of unary postfix operator
        private ExprNode ParseBinOpRHS(int exprPrec, ref ExprNode LHS)
        {
            while (true)
            {
                ExprOperator curOper = null;
                if (Current.Type != Op_Assignment)
                {
                    if (Current.Type != Operator)
                        return LHS;
                    curOper = Compiler.GetOperator(Current.Value);
                    if (curOper == null)
                    {
                        Error($"Operator '{Current.Value}' not defined");
                        return null;
                    }
                    // if this is a binop at least as tightly as the current binop,
                    // consume it, otherwise we are done
                    if (curOper.Precedence < exprPrec)
                        return LHS;
                }
                else
                {
                    curOper = new ExprOperator();
                }
                Lexer.Next();

                // Try to consume any possible expression
                var RHS = ParseUnaryPrefix();
                if (RHS == null)
                    return null;

                //if curOper binds less tightly with rhs than the operator after RHS,
                //let the pending operator take rhs as its LHS
                if (Current.Type == Operator)
                {
                    //TODO: Indented line skips
                    var nextOper = Compiler.GetOperator(Current.Value);
                    if (nextOper == null)
                        throw new exUnexpected($"'{Current.Value}' (Unknown operator)");

                    if (curOper.Precedence < nextOper.Precedence)
                    {
                        RHS = ParseBinOpRHS(curOper.Precedence + 1, ref RHS);
                        if (RHS == null)
                            return null;
                    }
                }
                LHS = new Expression(curOper, new List<ExprNode> { LHS, RHS });
            }
        }

        private ExprNode ParseUnaryPrefix()
        {
            if (Current.Type != Operator)
                return ParsePrimary();

            Lexer.Next();
            var oper = Compiler.GetOperator(Current.Value);
            if (oper == null)
                throw new exUnexpected($"'{Current.Value}' (Unknown operator)");

            var op = ParseUnaryPrefix();
            if (op != null)
                return new Expression(oper, new List<ExprNode> { op });

            return null;
        }

        private ExprNode ParseUnaryPostfix(ExprNode v) => throw new NotImplementedException();

        private ExprNode ParseAssignment(string name)
        {
            var var = (ExprNode)new VarNode(name);
            return ParseBinOpRHS(0, ref var);
        }

        #endregion

        #region Constants

        // numberexpr ::= number
        private IntegerNode ParseIntExpr()
        {
            Lexer.Next();
            return new IntegerNode(Lexer.Prev.Value);
        }

        #endregion

        #region Calls

        private ExprNode ParseConstructorCall() => throw new NotImplementedException();

        private ExprNode ParseIndexerCall(string name) => throw new NotImplementedException();

        private ExprNode ParseFunctionCall(string name)
        {
            Lexer.Next();
            var args = new List<ExprNode>();

            if (Current.Type != RoundBracketClose) //if we have args
            {
                while (true)
                {
                    var arg = ParseExpression();
                    if (arg != null)
                        args.Add(arg);
                    else
                        return null;

                    if (Current.Type == RoundBracketClose) // if we got closing bracket -> we are done
                        break;

                    if (Current.Value != ",") // if we got not separator -> error
                    {
                        Error("Expected ')' or ',' in arg list");
                        return null;
                    }
                    Lexer.Next(); // if comma -> getting next element
                    if (Current.Type == RoundBracketClose)
                    {
                        Error("Unexpected ')' after ','. Expected expression");
                    }
                }
            }

            Lexer.Next();

            return new CallFunctionNode(name, args); // return an argumented function call
        }

        #endregion

        #region Basis

        private ExprNode ParseExpression()
        {
            var lhs = ParseUnaryPrefix();
            if (lhs == null)
                return null;
            return ParseBinOpRHS(0, ref lhs);
        }

        /// <summary>
        /// Initialize parsing of any expression
        /// </summary>
        /// <returns></returns>
        private ExprNode ParsePrimary()
        {
            /*\
               Primary|>
               [•] Integer
               [ ] Float
               [ ] String
               [•] IdentifierExpr
               [•] ParenExpr
            \*/

            switch (Current.Type)
            {
                default:
                    Error($"Unexpected token {Current.Value}");
                    return null;

                //parenexpr
                case RoundBracketOpen:
                    return ParseParenExpr();

                //identifierexpr
                case Identifier:
                    return ParseIdentifierExpr();

                //intexpr
                case IntegerBinary:
                case IntegerOctal:
                case IntegerDecimal:
                case IntegerHexadecimal:
                    return ParseIntExpr();
            }
        }

        // parenexpr ::= '(' expression ')'
        private ExprNode ParseParenExpr()
        {
            Lexer.Next();
            var v = ParseExpression();
            if (v == null)
                return null;

            if (Current.Value != ")")
            {
                Error("Expected ')'");
                return null;
            }

            Lexer.Next(); // eat ')'
            if (Current.Type == Operator && Compiler.GetOperator(Current.Value).Kind == "postfix")
                return ParseUnaryPostfix(v);

            return v;
        }

        private ExprNode ParseIdentifierExpr()
        {
            /*\ IdentifierExpr|>
            [•] Get Variable     ::= Identifier
            [•] Assign Variable  ::= Identifier '='
            [•] Call function    ::= Identifier '(' expression* ')'
            [•] Call Indexer     ::= Identifier '[' expression* ']'
            [•] Call Constructor ::=    'new'   '(' expression* ')'
            \*/

            if (Current.Type == Key_New)
                return ParseConstructorCall();

            Lexer.Next();
            switch (Current.Type)
            {
                case RoundBracketOpen:
                    // Call Constructor
                    // Call Function
                    return ParseFunctionCall(Lexer.Prev.Value);

                // Call Indexer
                case SquareBracketOpen:
                    return ParseIndexerCall(Lexer.Prev.Value);

                // Assign Variable
                case Op_Assignment:
                    return ParseAssignment(Lexer.Prev.Value);
                default:
                    break;
            }

            // Get Variable
            if (Lexer.Prev.Type != Identifier)//if something went wrong | TODO: make "useless" parts skip
            {
                Error($"Unexpected token {Lexer.Prev.Value}");
                return null;
            }

            return new VarNode(Lexer.Prev.Value);
        }

        private List<VarDefinition> ParseFunctionArgs(eLexemType start, eLexemType end)
        {
            if (Current.Type == start)
                Lexer.Next();
            var args = new List<VarDefinition>();
            while (Current.Type != end)
            {
                if (Current.Value == ",")
                    Lexer.Next();
                Lexer.Next();
                var type = ParseType();
                args.Add(new VarDefinition(Current.Value, type));
                Lexer.Next();
            }
            if (Current.Type == end)
                Lexer.Next();
            Lexer.Next(); // consume '\n'
            return args;
        }

        #endregion

        #endregion

        #region Code Blocks

        /// <summary>
        /// Parses block of code
        /// </summary>
        private CodeBlock ParseBlock(int minIndent = 1)
        {
            /*\ Block |>
            [•] InitVariableExpr
            [•] ClearVariable
            [•] ParenExpr
            [•] IdentifierExpr
            [•] PrefixUnaryExpr
            |*\ flow-control statements
            |  [•] If
            |  [•] For
            |  [•] Foreach
            |  [•] While
            |  [•] Do While
            |  [•] Switch
            |  [•] Return
            |  [•] Try-Catch-Finally
            \*/

            //TODO: Make saving to list
            if (Current.Type != Indent) // no indent = no body
            {
                Lexer.SkipToEmptyLine();
                return null;
            }

            //Get size of indent
            var indent = Current.Value.Length;
            var exprs = new List<ExprNode>();
            var blocks = new List<CodeBlock>();
            Lexer.Next();
            while (indent == minIndent)
                switch (Current.Type)
                {
                    case NewLine:
                        Lexer.Next();
                        continue;
                    case Indent:
                        indent = Current.Value.Length;
                        if (indent > minIndent)
                        {
                            Warn($"Unexpected change of indent size. Expected {minIndent}, got {indent}");
                            blocks.Add(new ExprBlock(exprs, minIndent)); //save current block
                            exprs = new List<ExprNode>();
                            blocks.Add(ParseBlock(indent)); // parse new unexpected block
                        }
                        else if (indent < minIndent) // if indent decreased -> block ended
                            return new CodeBlock(blocks, minIndent);
                        Lexer.Next();
                        continue;

                    case Identifier:
                        var i = Lexer.I;
                        Lexer.Next();
                        var type = ParseType();

                        // InitVariableExpr
                        if (type != null)
                        {
                            if (Current.Type != Identifier)
                            {
                                Error($"Unexpected token '{Current.Value}'");
                                return null;
                            }
                            var name = Current.Value;
                            switch (Lexer.Next().Type)
                            {
                                case Op_Assignment:
                                    exprs.Add(ParseAssignment(type, name));
                                    break;
                                case Operator:
                                    exprs.Add(ParseExpression());
                                    break;
                                default:
                                    if (type == null)
                                        exprs.Add(ParseIdentifierExpr());
                                    else
                                        exprs.Add(new VarDefinition(name, type));
                                    break;
                            }
                            continue;
                        }

                        // IdentifierExpr
                        Lexer.I = i; //backtrack
                        exprs.Add(ParseIdentifierExpr()); //capture new expression
                        continue;

                    // ClearVariable
                    case Key_New:
                        exprs.Add(ParseConstructorCall());
                        continue;

                    // ParenExpr
                    case RoundBracketOpen:
                        exprs.Add(ParseParenExpr());
                        continue;

                    //PrefixUnaryExpr
                    case Operator:
                        exprs.Add(ParseUnaryPrefix());
                        continue;

                    //Return
                    case Key_Return:
                        if (exprs.Count > 0)
                            blocks.Add(new ExprBlock(exprs, indent));
                        blocks.Add(ParseReturn());
                        return new CodeBlock(blocks, indent);

                    //Branch
                    case Key_If:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseBranch());
                        continue;

                    //For loop
                    case Key_For:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseForLoop());
                        continue;

                    //Foreach loop
                    case Key_Foreach:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseForeachLoop());
                        continue;

                    //While loop
                    case Key_While:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseWhileLoop());
                        continue;

                    //Switch
                    case Key_Switch:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseSwitch());
                        continue;

                    //Do While Loop
                    case Key_Do:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseDoWhileLoop());
                        continue;

                    //Try-Catch-Finally
                    case Key_Try:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseTryCatchFinally());
                        continue;
                }
            return null;
        }

        private CodeBlock ParseTryCatchFinally() => throw new NotImplementedException();

        private CodeBlock ParseDoWhileLoop() => throw new NotImplementedException();

        private CodeBlock ParseSwitch() => throw new NotImplementedException();

        private CodeBlock ParseWhileLoop() => throw new NotImplementedException();

        private CodeBlock ParseForeachLoop() => throw new NotImplementedException();

        private CodeBlock ParseForLoop() => throw new NotImplementedException();

        private CodeBlock ParseBranch() => throw new NotImplementedException();

        private CodeBlock ParseReturn()
        {
            Lexer.Next();
            return new ReturnBlock(ParseExpression(), 0);
        }

        #endregion

        private ExprNode ParseAssignment(TypeNode t, string name)
        {
            var var = (ExprNode)new VarDefinition(name, t);
            return ParseBinOpRHS(0, ref var);
        }

        #endregion
    }
}
