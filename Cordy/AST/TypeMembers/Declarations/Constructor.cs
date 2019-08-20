namespace Cordy.AST
{
    public class Constructor : FunctionalMember
    {
        public Constructor(ConstructorDef def, BasicNode body)
            : base(def, body)
        { }

        public DefinedNode Empty { get; protected set; } = null;
    }
}
