namespace Cordy.AST
{
    using Codegen;
    /// <summary>
    /// Operator declaration
    /// </summary>
    public sealed class Operator : FunctionalMember
    {
        public Operator(OperatorDef def, BasicNode body)
            : base(def, body)
        {
            
            Kind = eNodeKind.Operator;
        }

        public override eNodeKind Kind { get; protected set; }

        public int GetPrecedence() => Compiler.GetOperPrecedence(this);

        protected internal override BasicNode Accept(Visitor visitor)
            => visitor.VisitOperator(this);

    }
}