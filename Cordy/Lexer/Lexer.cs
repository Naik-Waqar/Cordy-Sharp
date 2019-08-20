using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cordy
{
    using static eLexemType;
    using static RegexOptions;

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
            Compiled | IgnorePatternWhitespace | Multiline);

        private static readonly Regex BadNL
            = new Regex(@"\r\n?", Compiled);

        /// <summary>
        /// Makes a skip to the End-Of-Line character
        /// </summary>
        /// <returns>Skipped part of line</returns>
        internal List<Lexem> SkipToEOL()
        {
            var lex = new List<Lexem>();
            while (Current.Type != NewLine)
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
                    Indent,
                    new Regex(@"\t+",
                        Compiled)
                },
                #endregion

                #region Literals|String
                {
                    String,
                    new Regex(@"((\@)?|(\$)?|(\~)?|(\%)?)*?([""'`])(?(2)(?:(?(?=\6\6)\6\6|(?!\6)(?:.|\s))*)|(?:\\\6|(?:(?!\6)(.|\s)))*)\6",
                        Compiled)
                },
                #endregion

                #region Comments
                {
                    SingleLineComment,
                    new Regex(@"\#{2}(?:(?!\#{2}|\n|$).)*(?'end'\#{2})?",
                        Compiled)
                },
                {
                    MultiLineComment,
                    new Regex(@"\#\*(?:(?!\*\#)(?:.|\s))*(?'end'\*\#)",
                        Compiled)
                },
                #endregion

                #region Preprocessing
                {
                    PreprocessorDirective,
                    new Regex(@"\#(?:(?!\n)(?:(?'words'\w+)|\s+))*",
                        Compiled)
                },
                #endregion

                #region Keywords

                #region Storage Modifiers
                {
                    Key_AccessLevel,
                    new Regex(@"\b(?:public|private|internal)\b",
                        Compiled)
                },
                {
                    Key_Protected,
                    new Regex(@"\bprotected\b",
                        Compiled)
                },
                {
                    Key_Static,
                    new Regex(@"\bstatic\b",
                        Compiled)
                },
                {
                    Key_Sealed,
                    new Regex(@"\bsealed\b",
                        Compiled)
                },
                {
                    Key_FileContext,
                    new Regex(@"\bclass|enum|interface\b",
                        Compiled)
                },
                #endregion

                    #region Flow Control

                        #region Exception Handling
                {
                    Key_Try,
                    new Regex(@"\btry\b",
                        Compiled)
                },
                {
                    Key_Catch,
                    new Regex(@"\bcatch\b",
                        Compiled)
                },
                {
                    Key_Finally,
                    new Regex(@"\bfinally\b",
                        Compiled)
                },
                {
                    Key_Throw,
                    new Regex(@"\bthrow\b",
                        Compiled)
                },
                #endregion

                        #region Branching
                {
                    Key_If,
                    new Regex(@"\bif\b",
                        Compiled)
                },
                {
                    Key_ElseIf,
                    new Regex(@"\bel(?:se\s*)?if\b",
                        Compiled)
                },
                {
                    Key_Else,
                    new Regex(@"\belse\b",
                        Compiled)
                },
                {
                    Key_Switch,
                    new Regex(@"\bswitch\b",
                        Compiled)
                },
                        #endregion

                        #region Loops
                {
                    Key_Foreach,
                    new Regex(@"\bforeach\b",
                        Compiled)
                },
                {
                    Key_For,
                    new Regex(@"\bfor\b",
                        Compiled)
                },
                {
                    Key_While,
                    new Regex(@"\bwhile\b",
                        Compiled)
                },
                {
                    Key_Do,
                    new Regex(@"\bdo\b",
                        Compiled)
                },

                #endregion

                        #region Return
                {
                Key_Return,
                new Regex(@"\breturn\b",
                    Compiled)
                },
	                    #endregion

                    #endregion

                    #region Link Generation
                {
                    Key_Include,
                    new Regex(@"\binclude\b",
                        Compiled)
                },
                {
                    Key_Using,
                    new Regex(@"\busing\b",
                        Compiled)
                },
                #endregion

                    #region Property Modifiers
                {
                    Key_Get,
                    new Regex(@"\bget\b",
                        Compiled)
                },
                {
                    Key_Set,
                    new Regex(@"\bset\b",
                        Compiled)
                },
                #endregion

                    #region Memory Management
                {
                    Key_New,
                    new Regex(@"\bnew\b",
                        Compiled)
                },
                #endregion

                #endregion

                #region Literals

                    #region Identifier
                {
                    Identifier,
                    new Regex(@"\b(?:[a-zA-Z@$_]\w*.)*[a-zA-Z@$_]\w*\b",
                        Compiled)
                },
                    #endregion

                    #region Numbers
                {
                    Float,
                    new Regex(@"-?(?:\d[\d_]*)?[.,]\d[\d_]*",
                        Compiled)
                },
                {
                    IntegerBinary,
                    new Regex(@"-?0[bB][01][01_]*",
                        Compiled)
                },
                {
                    IntegerOctal,
                    new Regex(@"-?0[oO][0-7][0-7_]*",
                        Compiled)
                },
                {
                    IntegerHexadecimal,
                    new Regex(@"-?0[xX][\da-fA-F][\da-fA-F_]*",
                        Compiled)
                },
                {
                    IntegerDecimal,
                    new Regex(@"-?\d[\d_]*",
                        Compiled)
                },
                    #endregion

                    #region Operators
                {
                    Op_Assignment,
                    new Regex(@"(?<!.)=(?!.)",
                        Compiled)
                },
                {
                    Op_Comma,
                    new Regex(@"\,",
                        Compiled)
                },
                {
                    Operator,
                    new Regex(@"[<>=!#%^?:&*\-+\\$\/_~]{1,3}",
                        Compiled)
                },
                    #endregion

                    #region Brackets
                {
                    CurlyBracketOpen,
                    new Regex(@"\{",
                        Compiled)
                },
                {
                    RoundBracketOpen,
                    new Regex(@"\(",
                        Compiled)
                },
                {
                    SquareBracketOpen,
                    new Regex(@"\[",
                        Compiled)
                },
                {
                    CurlyBracketClose,
                    new Regex(@"\}",
                        Compiled)
                },
                {
                    RoundBracketClose,
                    new Regex(@"\)",
                        Compiled)
                },
                {
                    SquareBracketClose,
                    new Regex(@"\]",
                        Compiled)
                },
                    #endregion

                #endregion
            };

        private char InvertBracket(char c) => c switch
        {
            '{' => '}',
            '(' => ')',
            '[' => ']',
            '}' => '{',
            ')' => '(',
            ']' => '[',
            _ => '\0',
        };

        internal List<Lexem> GetUntil(string[] v, string ignore = null)
        {
            var counters = new Dictionary<char, int>
            {
                {'{', 0 },
                {'[', 0 },
                {'(', 0 },
            };

            var start = I;
            var undone = true;
            while (Current.Type != EOF && I < Lexems.Count && undone)
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
            while (Current.Type != NewLine && Prev.Type != NewLine)
                I++;

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
                new Lexem(SOF, Pos)
            };
            Lexems.AddRange(Tokenize(Text));
            Lexems.Add(new Lexem(EOF, Pos));
            I = 0;
        }

        public int Col, Row, I;

        internal bool Done { get; private set; }

        //public Lexem Prev { get; private set; }
        public Lexem Prev => Lexems[I - 1];

        //public Lexem Current { get; private set; }
        public Lexem Current => Lexems[I];

        public List<Lexem> Lexems = new List<Lexem>();

        private string tokenized => Text[0..(I + (I < Text.Length ? 1 : 0))];
        private char now => Text[I];

        private CordyType Type;

        public override string Stage { get; } = "Lexer";

        public override string FileName => Type.FullName;

        public override (int, int)? Pos => (Row + 1, Col + 1);

        public Lexem Next()
        {
            I++;
            if (I >= Lexems.Count)
                return Lexems[Lexems.Count - 1];
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
                    return new Lexem(NewLine, "\n", Pos);
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
