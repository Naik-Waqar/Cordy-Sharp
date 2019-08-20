namespace Cordy.AST
{
    public class Function : FunctionalMember, iEmpty<Function>
    {
        public Function(FunctionDef def, BasicNode body)
            : base(def, body)
        { }

        public static Function Empty { get; } = null;
    }
}