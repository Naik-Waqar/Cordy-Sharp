namespace Cordy.AST
{
    public sealed class Property : DefinedNode
    {
        public Property(PropertyDef def, BasicNode get, BasicNode set) : base(def)
        {
            Get = get;
            Set = set;
        }
        //TODO: Combine Get and Set to one block
        public BasicNode Get { get; }
        public BasicNode Set { get; }

        public DefinedNode Empty { get; protected set; } = null;

    }
}
