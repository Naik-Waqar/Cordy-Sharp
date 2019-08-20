namespace Cordy.AST
{
    /// <summary>
    /// Operator declaration
    /// </summary>
    public sealed class Operator : FunctionalMember
    {
        public Operator(OperatorDef def, BasicNode body)
            : base(def, body)
        { }

        public int GetPrecedence() => Compiler.GetOperPrecedence(this);

    }
}