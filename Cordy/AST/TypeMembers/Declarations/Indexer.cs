namespace Cordy.AST
{
    public class Indexer : FunctionalMember
    {
        public Indexer(IndexerDef def, BasicNode body)
            : base(def, body)
            => Kind = eNodeKind.Indexer;

        public override eNodeKind Kind { get; protected set; }
    }
}
