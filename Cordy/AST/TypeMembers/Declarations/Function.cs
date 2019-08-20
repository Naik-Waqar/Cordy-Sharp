namespace Cordy.AST
{
    public class Function : FunctionalMember
    {
        public Function(FunctionDef def, BasicNode body)
            : base(def, body)
        { }
    }
}