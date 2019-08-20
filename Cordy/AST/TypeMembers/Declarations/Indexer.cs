namespace Cordy.AST
{
    public class Indexer : FunctionalMember
    {
        public Indexer(IndexerDef def, BasicNode body)
            : base(def, body)
        { }

        public DefinedNode Empty { get; protected set; } = null;
    }
}
