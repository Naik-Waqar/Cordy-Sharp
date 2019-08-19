namespace Cordy.AST
{
    public class Constructor : FunctionalMember
    {
        public Constructor(ConstructorDef def, BasicNode body)
            : base(def, body)
            => Kind = eNodeKind.Operator;

        public override eNodeKind Kind { get; protected set; }
    }
}
