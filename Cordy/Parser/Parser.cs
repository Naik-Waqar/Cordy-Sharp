using Cordy.AST;
using Cordy.Codegen;
using System;
using System.Collections.Generic;

namespace Cordy
{
    //TODO: Comment all this stuff
    public class Parser : CompilerPart
    {
        private Lexer Lexer;
        private Listener Listener;

        internal Parser(CordyType type, Lexer lexer, Listener listener)
        {
            Type = type;
            Lexer = lexer;
            Listener = listener;
        }

        public Parser(Lexer lexer) => Lexer = lexer;

        private CordyType Type;

        private TypeNode handledType;
        private eAccessLevel lvl;
        private bool isProtected;
        private bool isStatic;
        private bool isSealed;
        private eTypeContext context;

        private Lexem Current => Lexer.Current;

        private List<string> Parameters { get; } = new List<string>();
        private List<string> Attributes { get; } = new List<string>();

        private List<string> typeParameters { get; } = new List<string>();
        private List<string> typeAttributes { get; } = new List<string>();

        public override string Stage { get; } = "Parser";
        public override string FileName => Type.FullName;
        public override (int, int) Pos => Current.Pos;

        private bool signatureParsed;

        /// <summary>
        /// Parsing in global context
        /// </summary>
        internal bool Parse()
        {
            ClearConsumables();

            //TODO: Use exceptions for error handling
            //TODO: Move switch to separate method
            while (true)
            {
                switch (Lexer.Next().Type)
                {
                    case eLexemType.EOF:
                        return true;

                    case eLexemType.Key_Include:
                        Lexer.Next();
                        if (Current.Type != eLexemType.String)
                        {
                            Error("'include' operator requires a string as it's argument");
                            Lexer.SkipToEOL();
                            continue;
                        }

                        continue;

                    #region Parameter
                    // ^{Parameter}^
                    // ^{Parameter}
                    //  {Parameter}
                    case eLexemType.CurlyBracketOpen:
                        ParseParameter();
                        continue;
                    #endregion

                    #region Attribute
                    // ^[Attribute]^
                    // ^[Attribute]
                    //  [Attribute]
                    case eLexemType.SquareBracketOpen:
                        ParseAttribute();
                        continue;
                    #endregion

                    //ignore trash
                    case eLexemType.NewLine:
                    case eLexemType.Indent:
                    case eLexemType.PreprocessorDirective:
                    case eLexemType.MultiLineComment:
                    case eLexemType.SingleLineComment:
                        continue;

                    #region Access Modifiers

                    //Access Level
                    case eLexemType.Key_AccessLevel:
                        if (lvl == eAccessLevel.Undefined)
                        {
                            lvl = (eAccessLevel)Enum.Parse(typeof(eAccessLevel), Current.Value, true);
                            continue;
                        }

                        Info($"Definition already have 'access level' modifier. Excess one ignored");
                        continue;

                    //Protected
                    case eLexemType.Key_Protected:
                        if (isProtected)
                            Info($"Definition already have 'protected' modifier. Excess one ignored");

                        isProtected = true;
                        continue;

                    //Static
                    case eLexemType.Key_Static:
                        if (isStatic)
                            Info($"Definition already have 'static' modifier. Excess one ignored");

                        isStatic = true;
                        continue;

                    //Sealed
                    case eLexemType.Key_Sealed:
                        if (signatureParsed)
                        {
                            Warn($"'sealed ' modifier can't be applied to definition. Ignored");
                            continue;
                        }

                        if (isSealed)
                            Info($"Type signature already have 'sealed' modifier. Excess one ignored");

                        isSealed = true;
                        continue;

                    #endregion

                    #region File context

                    //File Context
                    case eLexemType.Key_FileContext:
                        if (signatureParsed)
                        {
                            Error($"One type can't contain more than one signature. Processing failed");
                            return false;
                        }

                        //TODO: Add parents for type
                        //TODO: Add generic pattern for type
                        //TODO: Add settings for type
                        context = (eTypeContext)Enum.Parse(typeof(eTypeContext), Current.Value, true);
                        signatureParsed = true;
                        Lexer.SkipToEmptyLine();
                        ClearConsumables();
                        continue;

                    #endregion

                    #region Event

                    // Handle event
                    case eLexemType.Key_Event:
                        if (!signatureParsed || context == eTypeContext.Enum)
                        {
                            Error($"Event can be defined only in class or interface. Skipping declaration");
                            goto fail;
                        }
                        HandleEventDefinition();
                        ClearConsumables();
                        continue;

                    #endregion

                    #region Constructor

                    // Handle constructor
                    case eLexemType.Key_New:
                        if (!signatureParsed || context == eTypeContext.Enum)
                        {
                            Error($"Constructor can be defined only in class or interface. Skipping declaration");
                            goto fail;
                        }
                        HandleConstructorDefinition();
                        ClearConsumables();
                        continue;

                    #endregion

                    #region Indexer

                    // Handle indexer
                    case eLexemType.Key_This:
                        if (!signatureParsed || context == eTypeContext.Enum)
                        {
                            Error($"Indexer can be defined only in class or interface. Skipping declaration");
                            goto fail;
                        }
                        HandleIndexerDefinition();
                        ClearConsumables();
                        continue;

                    #endregion

                    #region Function and Property

                    //Function or property
                    case eLexemType.Identifier:
                        if (!signatureParsed || context == eTypeContext.Enum)
                        {
                            Error($"Function or property can be defined only in class or interface. Skipping declaration");
                            goto fail;
                        }

                        handle:
                        switch (Lexer.Next().Type)
                        {
                            case eLexemType.Indent: //TODO: Make type recognition more safe
                            case eLexemType.MultiLineComment:
                            case eLexemType.SingleLineComment:
                            case eLexemType.PreprocessorDirective:
                                goto handle;

                            //             id    '\n'
                            //             id    'is'
                            //             id    '='
                            //Property     ^name  ^checking
                            case eLexemType.NewLine: //TODO: Make NL skips
                            case eLexemType.Key_Is:
                            case eLexemType.Operator when Current.Value == "=":

                                HandlePropertyDefinition();
                                ClearConsumables();
                                continue;

                            //              id      '('
                            // Functiion    ^ name   ^checking
                            case eLexemType.RoundBracketOpen:
                                HandleFunctionDefinition();
                                ClearConsumables();
                                continue;

                            // id     '{'
                            // id     ':'
                            // id     id
                            // ^type   ^checking
                            // we got the type. If now is id -> use prev as type and current will be as name
                            case eLexemType.CurlyBracketOpen:
                            case eLexemType.Operator when Current.Value == ":":
                            case eLexemType.Identifier:
                                if (handledType != null)
                                {
                                    Error($"Definition can't have more than one type. Skipping declaration");
                                    goto fail;
                                }
                                handledType = ParseType();
                                if (handledType == null)
                                    goto fail;
                                break;


                            default:
                                Error($"Unexpected token '{Current.Value}'");
                                goto fail;
                        }
                        // we are here because handled the type
                        // if now is id -> checking for next with same rules
                        // if not -> error
                        if (Current.Type == eLexemType.Identifier)
                            goto handle;

                        fail:
                        Lexer.SkipToEmptyLine();
                        ClearConsumables();
                        continue;

                        #endregion
                }
            }
        }

        private void ParseAttribute()
        {
            if (Lexer.Prev.Value == "^")
            {
                typeAttributes.Add(ToStringBetween("[", "]"));
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
                typeParameters.Add(ToStringBetween("{", "}"));
                if (Current.Value == "^")
                    Lexer.Next();
                return;
            }
            Parameters.Add(ToStringBetween("{", "}"));
        }

        private string ToStringBetween(string start, string end)
        {
            var c = 1;
            var s = "";
            if (Current.Value == start)
                Lexer.Next();

            while (c > 0 && Current.Type != eLexemType.EOF)
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

        #region Handling

        public bool HandleFunctionDefinition()
        {
            Listener.EnterRule("HandleFunctionDefinition");
            var f = ParseFunction();
            Listener.ExitRule(f);
            if (f != null)
                Listener.Listen();
            else
            {
                //Err: Bad definition
                return false;
            }
            return true;
        }

        public bool HandlePropertyDefinition()
        {
            Listener.EnterRule("HandlePropertyDefinition");
            var p = ParseProperty();
            Listener.ExitRule(p);
            if (p != null)
                Listener.Listen();
            else
            {
                //Err: Bad definition
                return false;
            }
            return true;
        }

        public bool HandleIndexerDefinition() =>
            //Listener.EnterRule("HandleIndexerDefinition");
            ////var i = ParseIndexer();
            //object i = null;
            //Listener.ExitRule(i);
            //if (i != null)
            //    Listener.Listen();
            //else
            //{
            //    //Err: Bad definition
            //    return false;
            //}
            //return true;
            false;

        public bool HandleConstructorDefinition() =>
            //Listener.EnterRule("HandleConstructorDefinition");
            ////var c = ParseConstructor();
            //object c = null;
            //Listener.ExitRule(c);
            //if (c != null)
            //    Listener.Listen();
            //else
            //{
            //    //Err: Bad definition
            //    return false;
            //}
            //return true;
            false;

        public bool HandleEventDefinition() =>

            //Listener.EnterRule("HandleEventDefinition");
            ////var e = ParseEvent();
            //object e = null;
            //Listener.ExitRule(e);
            //if (e != null)
            //    Listener.Listen();
            //else
            //{
            //    //Err: Bad type
            //    return false;
            //}
            //return true;
            false;
        #endregion

        #region Parsing

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
                case eLexemType.Operator when Current.Value == ":":
                    Lexer.Next();
                    settings.Add(ParsePrimary());
                    goto check;
                case eLexemType.CurlyBracketOpen:
                    Lexer.Next();
                    r++;
                    template.AddRange(ParseTypeSequence(ref r));
                    goto check;

                case eLexemType.Indent:
                case eLexemType.NewLine:
                case eLexemType.MultiLineComment:
                case eLexemType.SingleLineComment:
                case eLexemType.PreprocessorDirective:
                    Lexer.Next();
                    goto check;

                case eLexemType.Identifier:
                case eLexemType.Operator:
                case eLexemType.CurlyBracketClose:
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
                    case eLexemType.CurlyBracketClose:
                        r--;
                        Lexer.Next();
                        return o;
                    case eLexemType.Operator when Current.Value == ",":
                        Lexer.Next();
                        break;
                }
                Lexer.Next();
                o.Add(ParseType(ref r));
            }
            return o;
        }

        private ExprNode ParseExpression()
        {
            var lhs = ParseUnaryPrefix();
            if (lhs == null)
                return null;
            return ParseBinOpRHS(0, ref lhs);
        }

        //TODO: Make parsing of unary postfix operator
        private ExprNode ParseBinOpRHS(int exprPrec, ref ExprNode LHS)
        {
            while (true)
            {
                if (Current.Type != eLexemType.Operator)
                    return LHS;
                var curOper = Compiler.GetOperator(Current.Value);
                if (curOper == null)
                {
                    Error($"Operator '{Current.Value}' not defined");
                    return null;
                }
                // if this is a binop at least as tightly as the current binop,
                // consume it, otherwise we are done
                if (curOper.Precedence < exprPrec)
                    return LHS;

                Lexer.Next();

                // Try to consume any possible expression
                var RHS = ParseUnaryPrefix();
                if (RHS == null)
                    return null;

                //if curOper binds less tightly with rhs than the operator after RHS,
                //let the pending operator take rhs as its LHS
                if (Current.Type != eLexemType.Operator)
                {
                    //TODO: Indented line skips
                    return LHS;
                }
                var nextOper = Compiler.GetOperator(Current.Value);
                if (nextOper == null)
                {
                    Error($"Unknown operator '{Current.Value}'");
                    return null;
                }
                if (curOper.Precedence < nextOper.Precedence)
                {
                    RHS = ParseBinOpRHS(curOper.Precedence + 1, ref RHS);
                    if (RHS == null)
                        return null;
                }
                LHS = new Expression(curOper, new List<ExprNode> { LHS, RHS });
            }
        }

        private ExprNode ParseUnaryPrefix()
        {
            if (Current.Type != eLexemType.Operator)
                return ParsePrimary();

            Lexer.Next();
            var oper = Compiler.GetOperator(Current.Value);
            if (oper == null)
            {
                Error($"Unknown operator '{Current.Value}'");
                return null;
            }

            var op = ParseUnaryPrefix();
            if (op != null)
                return new Expression(oper, new List<ExprNode> { op });

            return null;
        }

        // numberexpr ::= number
        private IntegerNode ParseIntExpr()
        {
            Lexer.Next();
            return new IntegerNode(Lexer.Prev.Value);
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
            Lexer.Next();
            if (Current.Type == eLexemType.Operator)
                return ParseUnaryPostfix(v);
            return v;
        }

        private ExprNode ParseUnaryPostfix(ExprNode v) => throw new NotImplementedException();

        private ExprNode ParseIdentifierExpr()
        {
            /*\ IdentifierExpr|>
            [•] Get Variable     ::= Identifier
            [•] Assign Variable  ::= Identifier '='
            [•] Call function    ::= Identifier '(' expression* ')'
            [•] Call Indexer     ::= Identifier '[' expression* ']'
            [•] Call Constructor ::=    'new'   '(' expression* ')'
            \*/

            if (Current.Type == eLexemType.Key_New)
                return ParseConstructorCall();

            Lexer.Next();
            switch (Current.Type)
            {
                case eLexemType.RoundBracketOpen:
                    // Call Constructor
                    // Call Function
                    return ParseFunctionCall(Lexer.Prev.Value);

                // Call Indexer
                case eLexemType.SquareBracketOpen:
                    return ParseIndexerCall(Lexer.Prev.Value);

                // Assign Variable
                case eLexemType.Operator when Current.Value == "=":
                    return ParseAssignment(Lexer.Prev.Value);
                default:
                    break;
            }

            // Get Variable
            if (Lexer.Prev.Type != eLexemType.Identifier)//if something went wrong
            {
                Error($"Unexpected token {Lexer.Prev.Value}");
                return null;
            }

            return new VarNode(Lexer.Prev.Value);
        }

        private ExprNode ParseConstructorCall() => throw new NotImplementedException();

        private ExprNode ParseAssignment(string name) => throw new NotImplementedException();

        private ExprNode ParseIndexerCall(string name) => throw new NotImplementedException();

        private ExprNode ParseFunctionCall(string name)
        {
            Lexer.Next();
            var args = new List<ExprNode>();

            if (Current.Type != eLexemType.RoundBracketClose) //if we have args
            {
                while (true)
                {
                    var arg = ParseExpression();
                    if (arg != null)
                        args.Add(arg);
                    else
                        return null;

                    if (Current.Type == eLexemType.RoundBracketClose) // if we got closing bracket -> we are done
                        break;

                    if (Current.Value != ",") // if we got not separator -> error
                    {
                        Error("Expected ')' or ',' in arg list");
                        return null;
                    }
                    Lexer.Next(); // if comma -> getting next element
                    if (Current.Type == eLexemType.RoundBracketClose)
                    {
                        Error("Unexpected ')' after ','. Expected expression");
                    }
                }
            }

            Lexer.Next();

            return new CallFunctionNode(name, args); // return an argumented function call
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
                case eLexemType.CurlyBracketOpen:
                    return ParseParenExpr();

                //identifierexpr
                case eLexemType.Identifier:
                    return ParseIdentifierExpr();

                //intexpr
                case eLexemType.IntegerBinary:
                case eLexemType.IntegerOctal:
                case eLexemType.IntegerDecimal:
                case eLexemType.IntegerHexadecimal:
                    return ParseIntExpr();
            }
        }


        /// <summary>
        /// Parses block of code
        /// </summary>
        /// <param name="current">Block that we are inside</param>
        /// <returns></returns>
        private CodeBlock ParseBlock(int minIndent)
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
            if (Current.Type != eLexemType.Indent) // no indent = no body
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
                    case eLexemType.Indent:
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

                    case eLexemType.Identifier:
                        var i = Lexer.I;
                        var type = ParseType();

                        // InitVariableExpr
                        if (type != null)
                        {
                            if (Current.Type != eLexemType.Identifier)
                            {
                                Error($"Unexpected token '{Current.Value}'");
                                return null;
                            }
                            exprs.Add(ParseAssignment(type));
                            continue;
                        }

                        // IdentifierExpr
                        Lexer.I = i; //backtrack
                        exprs.Add(ParseIdentifierExpr()); //capture new expression
                        continue;

                    // ClearVariable
                    case eLexemType.Key_New:
                        exprs.Add(ParseConstructorCall());
                        continue;

                    // ParenExpr
                    case eLexemType.RoundBracketOpen:
                        exprs.Add(ParseParenExpr());
                        continue;

                    //PrefixUnaryExpr
                    case eLexemType.Operator:
                        exprs.Add(ParseUnaryPrefix());
                        continue;

                    //Return
                    case eLexemType.Key_Return:
                        if (exprs.Count > 0)
                            blocks.Add(new ExprBlock(exprs, indent));
                        blocks.Add(ParseReturn());
                        return new CodeBlock(blocks, indent);

                    //Branch
                    case eLexemType.Key_If:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseBranch());
                        continue;

                    //For loop
                    case eLexemType.Key_For:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseForLoop());
                        continue;

                    //Foreach loop
                    case eLexemType.Key_Foreach:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseForeachLoop());
                        continue;

                    //While loop
                    case eLexemType.Key_While:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseWhileLoop());
                        continue;

                    //Switch
                    case eLexemType.Key_Switch:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseSwitch());
                        continue;

                    //Do While Loop
                    case eLexemType.Key_Do:
                        blocks.Add(new ExprBlock(exprs, indent));
                        exprs = new List<ExprNode>();
                        blocks.Add(ParseDoWhileLoop());
                        continue;

                    //Try-Catch-Finally
                    case eLexemType.Key_Try:
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

        private ExprNode ParseAssignment(TypeNode t) => throw new NotImplementedException();

        private CodeBlock ParseReturn()
        {
            Lexer.Next();
            return new ReturnBlock(ParseExpression(), 0);
        }

        private Function ParseFunction()
        {
            var name = Lexer.Prev.Value;
            var args = ParseArgListDef(eLexemType.RoundBracketOpen, eLexemType.RoundBracketClose);
            var body = ParseBlock(1);
            var def = new Definition(lvl, isProtected, isStatic, handledType, name, args);
            return new Function(def, body, Parameters, Attributes);
        }

        private List<VarDefinition> ParseArgListDef(eLexemType start, eLexemType end)
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

        private Property ParseProperty() => throw new NotImplementedException();

        #endregion
    }
}
