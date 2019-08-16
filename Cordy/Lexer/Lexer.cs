using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cordy
{
    public class Lexer : CompilerPart
    {
        //TODO: Add more different things
        private static readonly Regex Splitter
            = new Regex(@"(?:((@)?|(\$)?)*([""'`])(?(2)(?:(?(?=\4\4)\4\4|(?!\4)(?:.|\s))*)|(?:\\\4|(?:(?!\4)(.|\s)))*)\4)                                           # string                 #
                          |\n|\t+|\s+                                  # spaces                 #
                          |\#{2}(?:(?!\#{2}|\n|$).)*(?:\#{2})?          # single-line comment    #
                          |\#\*(?:(?!\*\#)(?:.|\s))*\*\#                # multi-line comment     #
                          |\#[^\n]*                                     # preprocessor directive #
                          |-?0?[bBoOxX]?[\da-fA-F_]+\b                  # integer                #
                          |(?:-?\d[\d_]*)?(?:[.,]\d[\d_]*)\b           # float                  #
                          |[<>=!#%\^?:&*.\-+\\$\/_~]{1,3}|,                # operator               #
                          |[]{()}[]                                     # bracket                #
                                                                        #========================#
                                                                        #        keywords        #
                                                                        #========================#
                          |\b(?:public|private|internal                 #  access level          #
                            |protected|static|sealed                    #  restrictions          #
                            |class|interface|enum                       #  file context          #
                            |try|catch|finally|throw                    #  exception handling    #
                            |if|el(?:se\s*)?if|else|switch              #  branching             #
                            |for(?:each)?|while|do|in                   #  loops                 #
                            |true|false                                 #  boolean constants     #
                            |include                                   #  import library        #
                            |[gs]et                                    #  property modifiers    #
                            |using                                      #  threading and rename  # <- Will be expanded later
                            |new                                        #  constructor           #
                            |event                                      #  event                 #
                            |is                                         #  assignment            #
                            |this                                       #  assignment            #
                          )\b                                           #========================#
                                                                        #      not keywords      #
                                                                        #========================#
                          |(?:[a-zA-Z@$_]\w*\.)*[a-zA-Z@$_]\w*\b         # identifier             #
                         ",
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);

        private static readonly Regex BadNL
            = new Regex(@"\r\n?", RegexOptions.Compiled);

        /// <summary>
        /// Makes a skip to the End-Of-Line character
        /// </summary>
        /// <returns>Skipped part of line</returns>
        internal List<Lexem> SkipToEOL()
        {
            var lex = new List<Lexem>();
            while (Current.Type != eLexemType.NewLine)
            {
                I++;
                lex.Add(Current);
            }
            return lex;
        }

        //TODO: Add 'event', 'is', 'this', 'in'
        private static readonly Dictionary<eLexemType, Regex> Regexes
            = new Dictionary<eLexemType, Regex>
            {
                #region Indentation
                {
                    eLexemType.Indent,
                    new Regex(@"\t+",
                        RegexOptions.Compiled)
                },
                #endregion

                #region Literals|String
                {
                    eLexemType.String,
                    new Regex(@"((\@)?|(\$)?|(\~)?|(\%)?)*?([""'`])(?(2)(?:(?(?=\6\6)\6\6|(?!\6)(?:.|\s))*)|(?:\\\6|(?:(?!\6)(.|\s)))*)\6",
                        RegexOptions.Compiled)
                },
                #endregion

                #region Comments
                {
                    eLexemType.SingleLineComment,
                    new Regex(@"\#{2}(?:(?!\#{2}|\n|$).)*(?'end'\#{2})?",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.MultiLineComment,
                    new Regex(@"\#\*(?:(?!\*\#)(?:.|\s))*(?'end'\*\#)",
                        RegexOptions.Compiled)
                },
                #endregion

                #region Preprocessing
                {
                    eLexemType.PreprocessorDirective,
                    new Regex(@"\#(?:(?!\n)(?:(?'words'\w+)|\s+))*",
                        RegexOptions.Compiled)
                },
                #endregion

                #region Keywords

                #region Storage Modifiers
                {
                    eLexemType.Key_AccessLevel,
                    new Regex(@"\b(?:public|private|internal)\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Protected,
                    new Regex(@"\bprotected\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Static,
                    new Regex(@"\bstatic\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Sealed,
                    new Regex(@"\bsealed\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_FileContext,
                    new Regex(@"\bclass|enum|interface\b",
                        RegexOptions.Compiled)
                },
                #endregion

                    #region Flow Control

                        #region Exception Handling
                {
                    eLexemType.Key_Try,
                    new Regex(@"\btry\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Catch,
                    new Regex(@"\bcatch\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Finally,
                    new Regex(@"\bfinally\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Throw,
                    new Regex(@"\bthrow\b",
                        RegexOptions.Compiled)
                },
                #endregion

                        #region Branching
                {
                    eLexemType.Key_If,
                    new Regex(@"\bif\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_ElseIf,
                    new Regex(@"\bel(?:se\s*)?if\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Else,
                    new Regex(@"\belse\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Switch,
                    new Regex(@"\bswitch\b",
                        RegexOptions.Compiled)
                },
                        #endregion

                        #region Loops
                {
                    eLexemType.Key_Foreach,
                    new Regex(@"\bforeach\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_For,
                    new Regex(@"\bfor\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_While,
                    new Regex(@"\bwhile\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Do,
                    new Regex(@"\bdo\b",
                        RegexOptions.Compiled)
                },

                #endregion

                        #region Return
                {
                eLexemType.Key_Return,
                new Regex(@"\breturn\b",
                    RegexOptions.Compiled)
                },
	                    #endregion

                    #endregion

                    #region Link Generation
                {
                    eLexemType.Key_Include,
                    new Regex(@"\binclude\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Using,
                    new Regex(@"\busing\b",
                        RegexOptions.Compiled)
                },
                #endregion

                    #region Property Modifiers
                {
                    eLexemType.Key_Get,
                    new Regex(@"\bget\b",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.Key_Set,
                    new Regex(@"\bset\b",
                        RegexOptions.Compiled)
                },
                #endregion

                    #region Memory Management
                {
                    eLexemType.Key_New,
                    new Regex(@"\bnew\b",
                        RegexOptions.Compiled)
                },
                #endregion

                #endregion

                #region Literals

                    #region Identifier
                {
                    eLexemType.Identifier,
                    new Regex(@"\b(?:[a-zA-Z@$_]\w*.)*[a-zA-Z@$_]\w*\b",
                        RegexOptions.Compiled)
                },
                    #endregion

                    #region Numbers
                {
                    eLexemType.Float,
                    new Regex(@"-?(?:\d[\d_]*)?[.,]\d[\d_]*",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.IntegerBinary,
                    new Regex(@"-?0[bB][01][01_]*",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.IntegerOctal,
                    new Regex(@"-?0[oO][0-7][0-7_]*",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.IntegerHexadecimal,
                    new Regex(@"-?0[xX][\da-fA-F][\da-fA-F_]*",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.IntegerDecimal,
                    new Regex(@"-?\d[\d_]*",
                        RegexOptions.Compiled)
                },
                    #endregion

                    #region Operators
                {
                    eLexemType.Operator,
                    new Regex(@"[<,>=!#%^?.:&*\-+\\$\/_~]{1,3}",
                        RegexOptions.Compiled)
                },
                    #endregion  

                    #region Brackets
                {
                    eLexemType.CurlyBracketOpen,
                    new Regex(@"\{",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.RoundBracketOpen,
                    new Regex(@"\(",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.SquareBracketOpen,
                    new Regex(@"\[",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.CurlyBracketClose,
                    new Regex(@"\}",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.RoundBracketClose,
                    new Regex(@"\)",
                        RegexOptions.Compiled)
                },
                {
                    eLexemType.SquareBracketClose,
                    new Regex(@"\]",
                        RegexOptions.Compiled)
                },
                    #endregion

                #endregion
            };

        private char InvertBracket(char c) => c switch
        {
            '{' => '}',
            '(' => ')',
            '[' => ']',
            '<' => '>',
            '}' => '{',
            ')' => '(',
            ']' => '[',
            '>' => '<',
            _ => '\0',
        };

        internal List<Lexem> GetUntil(string[] v, string ignore = null)
        {
            var counters = new Dictionary<char, int>
            {
                {'{', 0 },
                {'[', 0 },
                {'<', 0 },
                {'(', 0 },
            };

            var start = I;
            bool undone = true;
            while (Current.Type != eLexemType.EOF && I < Lexems.Count && undone)
            {
                var b = InvertBracket(Current.Value[0]);
                if (Current.Value == ignore)
                {
                    I++;
                    continue;
                }
                if (counters.ContainsKey(Current.Value[0]))
                    counters[Current.Value[0]]++;
                else if (counters.ContainsKey(b))
                    counters[b]--;

                if (v.Contains(Current.Value))
                {
                    undone = false;
                    foreach (var c in counters.Values)
                    {
                        if (c > 0)
                        {
                            undone = true;
                            break;
                        }
                    }
                }

                I++;
            }
            return Lexems.GetRange(start, I - start - 1);
        }

        /// <summary>
        /// Makes a skip to nearest two End-Of-Line characters
        /// </summary>
        internal string SkipToEmptyLine()
        {
            var start = I;
            while (Current.Type != eLexemType.NewLine && Prev.Type != eLexemType.NewLine)
                I++;
            //while (I < Text.Length && !(Text[I] == '\n' && Text[I - 1] == '\n'))
            //{
            //    I++;
            //    Row++;
            //}
            //Col = 0;
            //Prev = new Lexem(eLexemType.NewLine, 0, Row);
            //Current = Prev = new Lexem(eLexemType.NewLine, 1, Row);
            //return Text[start..I];
            return "";
        }

        private string Text;

        public void Prepare(CordyType type)
        {
            Type = type;
            Text = File.ReadAllText(type.FilePath);
            Text = BadNL.Replace(Text, "\n");
            Lexems = new List<Lexem>
            {
                new Lexem(eLexemType.SOF, Pos)
            };
            Lexems.AddRange(Tokenize(Text));
            Lexems.Add(new Lexem(eLexemType.EOF, Pos));
            I = 0;
        }

        public int Col, Row, I;

        internal bool Done { get; private set; }

        //public Lexem Prev { get; private set; }
        public Lexem Prev => Lexems[I - 1];

        //public Lexem Current { get; private set; }
        public Lexem Current => Lexems[I];

        private Match lastMatch;

        public List<Lexem> Lexems = new List<Lexem>();

        private string tokenized => Text[0..(I + (I < Text.Length ? 1 : 0))];
        private char now => Text[I];

        CordyType Type;

        public override string Stage { get; } = "Lexer";

        public override string FileName => Type.FullName;

        public override (int, int) Pos => (Row + 1, Col + 1);

        public Lexem Next()
        {
            I++;
            return Lexems[I];
        }

        private void SkipSpaces()
        {
            while (I < Text.Length && Text[I] == ' ') I++;
        }

        private Lexem MakeLexem(string v)
        {
            Lexem l = null;
            switch (v)
            {
                case "\n":
                    Col = 0;
                    Row++;
                    return new Lexem(eLexemType.NewLine, "\n", Pos);
                default:
                    if (v.Replace(" ", "").Length == 0)
                        break;
                    foreach (var r in Regexes)
                    {
                        if (r.Value.IsMatch(v))
                        {
                            l = new Lexem(r.Key, v.Replace(" ", ""), Pos);
                            break;
                        }
                    }
                    break;
            }
            Col += v.Length;
            I += v.Length;
            return l;
        }

        public List<Lexem> Tokenize(string s)
        {
            var ms = Splitter.Matches(s);
            var o = new List<Lexem>();
            foreach (Match m in ms)
            {
                var l = MakeLexem(m.Value);
                if (l != null)
                    o.Add(l);
            }
            return o;
        }
    }
}
