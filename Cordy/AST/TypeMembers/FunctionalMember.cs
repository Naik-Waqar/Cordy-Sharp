namespace Cordy.AST
{
    public abstract class FunctionalMember : DefinedNode
    {
        protected FunctionalMember(Definition def, BasicNode body)
            : base(def)
            => Body = body;

        public BasicNode Body { get; }
    }
}
