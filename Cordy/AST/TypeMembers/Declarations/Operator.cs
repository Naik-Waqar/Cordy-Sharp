namespace Cordy.AST
{
    /// <summary>
    /// Operator declaration
    /// </summary>
    public sealed class Operator : FunctionalMember, iEmpty<Operator>
    {
        public Operator(OperatorDef def, BasicNode body)
            : base(def, body)
        { }

        internal ExprOperator ToExprOperator()
            => new ExprOperator((Definition as OperatorDef).MetaParts);

        public static Operator Empty { get; } = null;
    }
}