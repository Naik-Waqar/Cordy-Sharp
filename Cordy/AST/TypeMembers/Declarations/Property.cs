namespace Cordy.AST
{
    public sealed class Property : DefinedNode
    {
        public Property(PropertyDef def, BasicNode get, BasicNode set) : base(def)
        {
            Get = get;
            Set = set;

        }

        public BasicNode Get { get; }
        public BasicNode Set { get; }

    }
}
