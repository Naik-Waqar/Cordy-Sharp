using System;
using System.Collections.Generic;

namespace Cordy
{
    internal class CordyPreParser
    {
        public CordyPreParser(string filename) => FileName = filename;

        private string FileName;

        internal bool PreParse(CordyType type, Lexer lexer)
        {
            if (!ParseSignature(type, lexer))
                return false;

            var parameters = new List<List<Lexem>>();
            var attributes = new List<List<Lexem>>();

            while (lexer.Current.Type != eLexemType.EOF)
            {
                if (ConsumeDefinition(type, lexer, ref parameters, ref attributes))
                {
                    parameters = new List<List<Lexem>>();
                    attributes = new List<List<Lexem>>();
                    continue;
                }
                else
                    lexer.SkipToEmptyLine();
            }
            return true;
        }

        private bool ConsumeDefinition(CordyType type, Lexer lexer, ref List<List<Lexem>> parameters, ref List<List<Lexem>> attributes)
        {
            parameters ??= new List<List<Lexem>>();
            attributes ??= new List<List<Lexem>>();

            var lvl = eAccessLevel.Undefined;
            bool isStatic = type.isStatic, isProtected = type.isProtected;
            var defType = new List<Lexem>();
            Lexem identifier = null;
            PreParsedDef d;

            lexer.SkipToEOL();
        meta:
            switch (lexer.Next().Type)
            {

                case eLexemType.PreprocessorDirective:
                case eLexemType.Indent:
                case eLexemType.MultiLineComment:
                case eLexemType.SingleLineComment:
                case eLexemType.NewLine:
                case eLexemType.Operator when lexer.Current.Value == "^":
                    goto meta;

                default: break;
            }

            ConsumeAccessLevel(lexer, ref lvl);
            ConsumeProtected(lexer, ref isProtected);
            ConsumeStatic(lexer, ref isStatic);

            if (ConsumeType(lexer, ref defType) && defType != null && defType.Count == 1)
            {
                switch (defType[0].Type)
                {
                    #region Constructor
                    //Constructor:: 'new' '(' args* ')'
                    case eLexemType.Key_New:
                        // always returns an instance of type where defined
                        d = MakeConstructorDefinition(lvl, isProtected, isStatic, type.Name);

                        goto define;
                    #endregion

                    #region Indexer
                    //Indexer:: type? 'this' '[' args* ']'
                    case eLexemType.Key_This:
                        // we didn't got the return type, so we will use the type that we are parsing now
                        //TODO: Deprecate auto type assignment in generic type
                        d = MakeIndexerDefinition(lvl, isProtected, isStatic, type.Name);

                        goto define;
                    #endregion

                    case eLexemType.Identifier:
                        // it's possible that 'defType' can be a type
                        // it means:
                        // next == 'identifier' => function or property with type
                        // next == 'round bracket' => void function
                        // next == '=' or 'is' or '\n' => property with 'this' type
                        // next == 'oper' => operator with type
                        // next == 'event' => event with type
                        // next == 'something else' => goto fail
                        break;

                    #region Event
                    //Event:: type? 'event' name '(' args* ')'
                    case eLexemType.Key_Event:
                        // we didn't got the return type, so we will use 'void'
                        d = MakeEventDefinition(lvl, isProtected, isStatic, "void");

                        goto define;
                    #endregion

                    #region Operator
                    //Operator:: type? oper '(' args* ')'
                    case eLexemType.Operator:
                        if (isStatic)
                            Compiler.Info($"Operators doesn't need a 'static' modifier, because they are always marked as 'static'", FileName, lexer.Current.Pos, "PreParser");

                        // we didn't got the return type, so we will use the type that we are parsing now
                        d = MakeOperatorDefinition(lexer, lvl, isProtected, type.Name, defType[0].Value);
                        goto define;
                        #endregion

                }
            }

            // possible output is 'operator', 'identifier', 'new line', 'is' or 'round bracket'.
            // if 'round bracket', then parsing function
            // if 'is' or 'new line' or 'operator' is assignment, then parsing property
            // if 'operator' but not assignment, then parsing operator
            // otherwise failed
            ConsumeIdentifier(lexer, ref identifier);
            if (identifier == null)
                goto fail; // next == 'something else'

            switch (identifier.Type)
            {
                #region Function
                //Function:: type? name '(' args* ')'
                case eLexemType.RoundBracketOpen:
                    //if not found possible element for identifier, then assume that we parsing function with name stored in defType
                    if (defType.Count == 1 && defType[0].Type == eLexemType.Identifier) // identifier stored in type?
                    {
                        //By default as a return type will be a void type
                        d = MakeFunctionDefinition(lexer, lvl, isProtected, isStatic, "void", defType[0].Value);
                        goto define;
                    }
                    break;
                #endregion

                #region Property

                //Property:: type? name ( 'is' value )?
                //Property:: type? name ( '=' value )?
                //Property:: type? name \n
                case eLexemType.NewLine:
                case eLexemType.Operator when identifier.Value == "=":
                case eLexemType.Key_Is:
                    d = MakePropertyDefinition(lexer, lvl, isProtected, isStatic, type.Name, identifier.Value);
                    goto define;
                #endregion

                #region Operator

                //Operator:: type? name '(' args* ')'
                case eLexemType.Operator:
                    //now we have the type
                    lexer.Next();
                    d = MakeOperatorDefinition(lexer, lvl, isProtected, defType, identifier.Value);
                    goto define;
                #endregion

                case eLexemType.Identifier: // it can be function or property
                    break;

                #region Event
                //Event:: type? 'event' name '(' args* ')'
                case eLexemType.Key_Event:
                    d = MakeEventDefinition(lvl, isProtected, isStatic, defType);
                    goto define;
                #endregion

                default:
                    Compiler.Error("Unknown definition. Moving to first empty line", FileName, identifier.Pos, "PreParser");
                    lexer.SkipToEmptyLine();
                    goto fail;
            }

            //there left only 3 possible ends:
            // function
            // property
            // fail

            switch (lexer.Current.Type)
            {
                #region Function
                //Function:: type? name '(' args* ')'
                case eLexemType.RoundBracketOpen:
                    //if not found possible element for identifier, then assume that we parsing function with name stored in defType
                    if (defType.Count == 1 && defType[0].Type == eLexemType.Identifier) // identifier stored in type?
                    {
                        //By default as a return type will be a void type
                        d = MakeFunctionDefinition(lexer, lvl, isProtected, isStatic, defType, identifier);
                        goto define;
                    }
                    goto fail;
                #endregion

                #region Property

                //Property:: type? name ( 'is' value )?
                //Property:: type? name ( '=' value )?
                //Property:: type? name \n
                case eLexemType.NewLine:
                case eLexemType.Operator when identifier.Value == "=":
                case eLexemType.Key_Is:
                    d = MakePropertyDefinition(lexer, lvl, isProtected, isStatic, defType, identifier);

                    goto define;
                #endregion

                default: goto fail;
            }

        define:
            if (d == null)
                goto fail;
            d.Parameters = parameters;
            d.Attributes = attributes;
            type.Definitions.Add(d);

            parameters = new List<List<Lexem>>();
            attributes = new List<List<Lexem>>();
            return true;

        fail:
            return false;
        }

        private string TypeToString(List<Lexem> defType)
        {
            var t = "";
            foreach (var f in defType)
                t += f.Value;

            return t;
        }

        private PreParsedDef MakeFunctionDefinition(Lexer lexer, eAccessLevel lvl, bool isProtected, bool isStatic, List<Lexem> defType, Lexem identifier)
            => MakeFunctionDefinition(lexer, lvl, isProtected, isStatic, TypeToString(defType), identifier.Value);

        private PreParsedDef MakeFunctionDefinition(Lexer lexer, eAccessLevel lvl, bool isProtected, bool isStatic, string type, string name)
        {
            var args = ConsumeBetween(lexer, "(", ")", "<");
            var bodyStart = lexer.I;
            lexer.SkipToEmptyLine();
            var bodyEnd = lexer.I;
            return new PreParsedDef
            {
                AccesModifier = lvl,
                Args = args,
                isProtected = isProtected,
                isStatic = isStatic,
                Length = bodyEnd - bodyStart,
                Start = bodyStart,
                Name = name,
                Type = type,
                Kind = eDefinitionType.Function
            };
        }

        private PreParsedDef MakePropertyDefinition(Lexer lexer, eAccessLevel lvl, bool isProtected, bool isStatic, List<Lexem> defType, Lexem identifier)
            => MakePropertyDefinition(lexer, lvl, isProtected, isStatic, TypeToString(defType), identifier.Value);

        private PreParsedDef MakePropertyDefinition(Lexer lexer, eAccessLevel lvl, bool isProtected, bool isStatic, string type, string name)
        {
            var args = lexer.SkipToEOL();
            var bodyStart = lexer.I;
            lexer.SkipToEmptyLine();
            var bodyEnd = lexer.I;
            return new PreParsedDef
            {
                AccesModifier = lvl,
                isProtected = isProtected,
                isStatic = isStatic,
                Length = bodyEnd - bodyStart,
                Start = bodyStart,
                Args = args, // default value
                Name = name,
                Type = type,
                Kind = eDefinitionType.Property
            };
        }

        private PreParsedDef MakeOperatorDefinition(Lexer lexer, eAccessLevel lvl, bool isProtected, List<Lexem> type, string code)
            => MakeOperatorDefinition(lexer, lvl, isProtected, TypeToString(type), code);

        private PreParsedDef MakeOperatorDefinition(Lexer lexer, eAccessLevel lvl, bool isProtected, string type, string code)
        {
            var args = ConsumeBetween(lexer, "(", ")");
            var bodyStart = lexer.I;
            lexer.SkipToEmptyLine();
            var bodyEnd = lexer.I;
            return new PreParsedDef
            {
                AccesModifier = lvl,
                isProtected = isProtected,
                isStatic = true,
                Length = bodyEnd - bodyStart,
                Start = bodyStart,
                Args = args, // args
                Name = code,
                Type = type,
                Kind = eDefinitionType.Operator
            };
        }

        private PreParsedDef MakeEventDefinition(eAccessLevel lvl, bool isProtected, bool isStatic, string name)
        {
            throw new NotImplementedException();
        }

        private PreParsedDef MakeEventDefinition(eAccessLevel lvl, bool isProtected, bool isStatic, List<Lexem> defType)
        {
            throw new NotImplementedException();
        }

        private PreParsedDef MakeIndexerDefinition(eAccessLevel lvl, bool isProtected, bool isStatic, string name)
        {
            throw new NotImplementedException();
        }

        private PreParsedDef MakeConstructorDefinition(eAccessLevel lvl, bool isProtected, bool isStatic, string name)
        {
            throw new NotImplementedException();
        }

        private void ConsumeAccessLevel(Lexer lexer, ref eAccessLevel lvl)
        {
        sw:
            switch (lexer.Current.Type)
            {
                case eLexemType.Indent:
                case eLexemType.PreprocessorDirective:
                case eLexemType.SingleLineComment:
                case eLexemType.MultiLineComment:
                case eLexemType.NewLine:
                    Compiler.Info($"Highly unrecommended to write comments, place preprocessor directives, 'new lines' and indents in definition.", FileName, lexer.Current.Pos, "PreParser");
                    lexer.Next();
                    goto sw;

                case eLexemType.Key_AccessLevel:
                    if (lvl != eAccessLevel.Undefined)
                    {
                        Compiler.Warn($"Definition already have 'access level' modifier. Excess one ignored", FileName, lexer.Current.Pos, "PreParser");
                        lexer.Next();
                        return;
                    }
                    lvl = (eAccessLevel)Enum.Parse(typeof(eAccessLevel), lexer.Current.Value, true);
                    lexer.Next();
                    return;
                case eLexemType.Key_Static:
                case eLexemType.Key_Protected:
                case eLexemType.Identifier:
                case eLexemType.Operator:
                    return;
                default:
                    Compiler.Warn($"Unexpected token '{lexer.Current.Value}' ignored", FileName, lexer.Current.Pos, "PreParser");
                    lexer.Next();
                    return;
            }
        }

        private void ConsumeProtected(Lexer lexer, ref bool isProtected)
        {
        sw:
            switch (lexer.Current.Type)
            {
                case eLexemType.Indent:
                case eLexemType.PreprocessorDirective:
                case eLexemType.SingleLineComment:
                case eLexemType.MultiLineComment:
                case eLexemType.NewLine:
                    Compiler.Info($"Highly unrecommended to write comments, place preprocessor directives, 'new lines' and indents in definition.", FileName, lexer.Current.Pos, "PreParser");
                    lexer.Next();
                    goto sw;

                case eLexemType.Key_Protected:
                    if (isProtected)
                        Compiler.Warn($"Definition already have 'protected' modifier. Excess one ignored", FileName, lexer.Current.Pos, "PreParser");
                    isProtected = true;
                    lexer.Next();
                    return;

                case eLexemType.Key_AccessLevel:
                case eLexemType.Key_Static:
                case eLexemType.Identifier:
                case eLexemType.Operator:
                    return;
                default:
                    Compiler.Warn($"Unexpected token '{lexer.Current.Value}' ignored", FileName, lexer.Current.Pos, "PreParser");
                    lexer.Next();
                    return;
            }
        }
        private void ConsumeStatic(Lexer lexer, ref bool isStatic)
        {
        sw:
            switch (lexer.Current.Type)
            {
                case eLexemType.Indent:
                case eLexemType.PreprocessorDirective:
                case eLexemType.SingleLineComment:
                case eLexemType.MultiLineComment:
                case eLexemType.NewLine:
                    Compiler.Info($"Highly unrecommended to write comments, place preprocessor directives, 'new lines' and indents in definition.", FileName, lexer.Current.Pos, "PreParser");
                    lexer.Next();
                    goto sw;

                case eLexemType.Key_Static:
                    if (isStatic)
                        Compiler.Warn($"Definition already have 'static' modifier. Excess one ignored", FileName, lexer.Current.Pos, "PreParser");
                    isStatic = true;
                    lexer.Next();
                    return;

                case eLexemType.Key_AccessLevel:
                case eLexemType.Key_Protected:
                case eLexemType.Identifier:
                case eLexemType.Operator:
                    return;
                default:
                    Compiler.Warn($"Unexpected token '{lexer.Current.Value}' ignored", FileName, lexer.Current.Pos, "PreParser");
                    lexer.Next();
                    return;
            }
        }

        private bool ConsumeType(Lexer lexer, ref List<Lexem> type)
        {
        sw:
            switch (lexer.Current.Type)
            {
                case eLexemType.RoundBracketOpen:
                    type = ConsumeBetween(lexer, "(", ")");
                    return true;
                case eLexemType.Identifier:
                    type.Add(lexer.Current);
                    switch (lexer.Next().Type)
                    {
                        case eLexemType.Identifier:
                            lexer.I--;
                            return true;
                        case eLexemType.Operator when lexer.Current.Value == "<":
                            if (lexer.Next().Type != eLexemType.RoundBracketOpen)
                                type.AddRange(ConsumeBetween(lexer, "<", ">"));
                            return true;
                        case eLexemType.Operator:
                            return true;

                        default:
                            goto fail;
                    }
                case eLexemType.Key_New:
                case eLexemType.Key_This:
                case eLexemType.Key_Event:
                case eLexemType.Operator:
                    type.Add(lexer.Current);
                    return true;

                case eLexemType.NewLine:
                case eLexemType.PreprocessorDirective:
                case eLexemType.MultiLineComment:
                case eLexemType.SingleLineComment:
                case eLexemType.Indent:
                    Compiler.Info($"Highly unrecommended to write comments, place preprocessor directives, 'new lines' and indents in definition.", FileName, lexer.Current.Pos, "PreParser");
                    lexer.Next();
                    goto sw;
            }
        fail:
            Compiler.Error($"Unexpected token '{lexer.Current.Value}'. Bad definition. Coming to nearest empty line", FileName, lexer.Current.Pos, "PreParser");
            lexer.SkipToEmptyLine();
            type = null;
            return false;
        }

        private void ConsumeIdentifier(Lexer lexer, ref Lexem identifier)
        {
        // possible output is 'operator', 'identifier', 'new line', 'is' or 'round bracket'.
        // if 'round bracket', then parsing function
        // if 'is' or 'new line' or 'operator' is assignment, then parsing property
        // if 'operator' but not assignment, then parsing operator
        // otherwise failed
        sw:
            switch (lexer.Current.Type)
            {
                case eLexemType.NewLine:
                    Compiler.Info($"Highly unrecommended to write comments, place preprocessor directives, 'new lines' and indents in definition.", FileName, lexer.Current.Pos, "PreParser");
                    goto case eLexemType.Identifier;

                case eLexemType.RoundBracketOpen:
                case eLexemType.Key_Is:
                case eLexemType.Operator:
                case eLexemType.Identifier:
                    identifier = lexer.Current;
                    return;

                case eLexemType.PreprocessorDirective:
                case eLexemType.MultiLineComment:
                case eLexemType.SingleLineComment:
                case eLexemType.Indent:
                    Compiler.Info($"Highly unrecommended to write comments, place preprocessor directives, 'new lines' and indents in definition.", FileName, lexer.Current.Pos, "PreParser");
                    lexer.Next();
                    goto sw;
                default:
                    Compiler.Error($"Unexpected token '{lexer.Current.Value}'. Bad definition. Coming to nearest empty line", FileName, lexer.Current.Pos, "PreParser");
                    lexer.SkipToEmptyLine();
                    return;
            }
        }

        private List<Lexem> ConsumeBetween(Lexer l, string start, string end, string ignore = null)
        {
            if (l.Current.Value == start)
                l.Next();
            var lex = l.GetUntil(new[] { end }, ignore);
            if (l.Current.Value == end)
                l.Next();
            return lex;
        }

        // Any codeblock ends on empty line or when indentation became shorter
        //private List<Lexem> GetBody(Lexer lexer) => lexer.Tokenize(lexer.SkipToEmptyLine());

        //private List<Lexem> GetArgs(Lexer lexer, char end) => lexer.GetUntil(new[] { end });

        private List<Lexem> GetType(Lexer lexer, int gens = 0)
        {
            var parts = new List<Lexem>
            {
                lexer.Current
            };
            while (true)
            {
                switch (lexer.Next()?.Type)
                {
                    case eLexemType.Identifier when gens == 0:
                        return parts;

                    case eLexemType.Comma when gens > 0:
                        parts.Add(lexer.Current);
                        break;

                    case eLexemType.Comma:
                        Compiler.Error($"Unexpected token '{lexer.Current}", FileName, lexer.Pos, "PreParser");
                        return parts;

                    case eLexemType.Operator when lexer.Current.Value == "<":
                        parts.AddRange(GetType(lexer, gens++));
                        gens--;
                        break;
                    case eLexemType.Operator when lexer.Current.Value == ">":
                        return parts;

                    case eLexemType.SquareBracketOpen when lexer.Current.Value == "[":
                        parts.Add(lexer.Current);
                        while (lexer.Next().Value != "]")
                        {
                            if (lexer.Current.Type == eLexemType.EOF)
                            {
                                Compiler.Error($"Unclosed bracket of array initializer\n\t ']' expected", FileName, lexer.Pos, "PreParser");
                                break;
                            }
                            if (lexer.Current.Type != eLexemType.Comma)
                            {
                                Compiler.Error($"Unexpected token '{lexer.Current.Value}' in array initializer\n\t ']' or ',' expected", FileName, lexer.Pos, "PreParser");
                                break;
                            }
                            parts.Add(lexer.Current);
                        }
                        parts.Add(lexer.Current);
                        break;

                    case eLexemType.NewLine:
                        Compiler.Error("Unexpected 'new line' in type name\n\t This type can break code", FileName, lexer.Pos, "PreParser");
                        return parts;

                    case eLexemType.SquareBracketOpen when lexer.Current.Value == "(":
                        Compiler.Error("Tuples not supported yet\n\t", FileName, lexer.Pos, "PreParser");
                        return parts;
                }
            }
        }

        internal bool ParseSignature(CordyType type, Lexer lexer)
        {
            // in best case we expect only one type context ('class', 'interface', 'enum')
            // in worst case we expect on first line something like
            // internal protected static sealed class<T, T>{type name, type name, '...'} : type<T<T<T>>{'any value'}>, type<T<T<T>>{'any value'}>, '...'

            //all properties and attributes can be written later by ^{Parameter} or ^[Attribute] on any line (recommended to use it right after the signature)

            var lvl = eAccessLevel.Undefined;
            bool isStatic = false, isSealed = false, isProtected = false;

        sw:
            lexer.Next();
            switch (lexer.Current.Type)
            {
                case eLexemType.Key_AccessLevel when lvl != eAccessLevel.Undefined:
                    Compiler.Warn($"Duplication of 'access level' modifier\n\t Excess modifier ignored", FileName, lexer.Pos, "PreParser");
                    goto sw;

                case eLexemType.Key_AccessLevel:
                    //TODO: Make code style settings
                    //if (isStatic)
                    //    Compiler.Warn($"'static' modifier must be after 'access level' modifier", FileName, lexer.Pos, "PreParser");
                    //
                    //if (isSealed)
                    //    Compiler.Warn($"'sealed' modifier must be after 'access level' modifier", FileName, lexer.Pos, "PreParser");
                    //
                    //if (isProtected)
                    //    Compiler.Warn($"'protected' modifier must be after 'access level' modifier", FileName, lexer.Pos, "PreParser");

                    lvl = (eAccessLevel)Enum.Parse(typeof(eAccessLevel), lexer.Current.Value, true);
                    if (lvl == eAccessLevel.Private)
                    {
                        Compiler.Warn($"Type can't be marked as 'private'\n\t Using 'internal' instead", FileName, lexer.Pos, "PreParser");
                        lvl = eAccessLevel.Internal;
                    }
                    goto sw;

                case eLexemType.Key_Protected when isProtected:
                    Compiler.Warn($"Duplication of 'protected' modifier\n\t Excess modifier ignored", FileName, lexer.Pos, "PreParser");
                    goto sw;

                case eLexemType.Key_Sealed when isSealed:
                    Compiler.Warn($"Duplication of 'sealed' modifier\n\t Excess modifier ignored", FileName, lexer.Pos, "PreParser");
                    goto sw;

                case eLexemType.Key_Static when isStatic:
                    Compiler.Warn($"Duplication of 'static' modifier\n\t Excess modifier ignored", FileName, lexer.Pos, "PreParser");
                    goto sw;

                case eLexemType.Key_Protected:
                    //if (isStatic)
                    //    Compiler.Warn($"'static' modifier must be placed after 'protected' or 'access level' modifier", FileName, lexer.Pos, "PreParser");

                    if (isSealed)
                    {
                        //Compiler.Warn($"'sealed' modifier must be placed after 'static' or 'access level' modifier", FileName, lexer.Pos, "PreParser");
                        Compiler.Warn($"Combination of 'protected' and 'sealed' modifiers is impossible\n\t 'sealed' ignored", FileName, lexer.Pos, "PreParser");
                        isSealed = false;
                    }

                    isProtected = true;
                    goto sw;

                case eLexemType.Key_Sealed:
                    isSealed = true;
                    goto sw;

                case eLexemType.Key_Static:
                    isStatic = true;
                    goto sw;

                case eLexemType.Key_FileContext:
                    if (lvl == eAccessLevel.Undefined)
                    {
                        Compiler.Warn($"Access level for type is not specified\n\t Using 'internal'", FileName, lexer.Pos, "PreParser");
                        lvl = eAccessLevel.Internal;
                    }

                    type.AccessLevel = lvl;
                    type.isStatic = isStatic;
                    type.isSealed = isSealed;
                    type.isProtected = isProtected;
                    type.Context = (eTypeContext)Enum.Parse(typeof(eTypeContext), lexer.Current.Value, true);

                    //TODO: Make generic recognition
                    //TODO: Make settings recognition
                    //TODO: Make parent recognition
                    //ParseRestOfSignature();
                    lexer.SkipToEOL();
                    return true;
                case eLexemType.NewLine:
                    goto sw;
                default:
                    Compiler.Error($"Unexpected token {lexer.Current?.Value}\n\t Bad type signature. Unable to continue file processing", FileName, lexer.Pos, "PreParser");
                    goto sw;
            }
        }
    }
}
