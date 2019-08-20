namespace Cordy
{
    public class Lexem
    {
        public string Value { get; internal set; }

        public eLexemType Type { get; }

        public (int Row, int Col)? Pos { get; }

        public Lexem(eLexemType type, (int row, int col)? pos) : this(type, null, pos) { }

        public Lexem(eLexemType type, string val, (int row, int col)? pos)
        {
            Type = type;
            Value = val;
            Pos = pos;
        }
    }
    public enum eLexemType
    {
        NewLine,
        Indent,
        String,
        SingleLineComment,
        MultiLineComment,
        PreprocessorDirective,
        Identifier,
        IntegerBinary,
        IntegerOctal,
        IntegerHexadecimal,
        IntegerDecimal,
        Float,
        Operator,
        SquareBracketOpen,

        Key_AccessLevel,
        Key_Protected,
        Key_Static,
        Key_Sealed,
        Key_Try,
        Key_Catch,
        Key_New,
        Key_Set,
        Key_Get,
        Key_Using,
        Key_Include,
        Key_Do,
        Key_While,
        Key_For,
        Key_Foreach,
        Key_Switch,
        Key_Else,
        Key_ElseIf,
        Key_If,
        Key_Throw,
        Key_Finally,
        Key_FileContext,
        EOF,
        Key_This,
        Key_Event,
        Key_Is,
        Op_Comma,
        Undefined,
        SOF,
        CurlyBracketOpen,
        RoundBracketOpen,
        SquareBracket,
        AngleBracketOpen,
        AngleBracketClose,
        CurlyBracketClose,
        RoundBracketClose,
        SquareBracketClose,
        Key_Return,
        Op_Assignment
    }
}
