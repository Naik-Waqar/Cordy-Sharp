using System.Collections.Generic;

namespace Cordy.AST
{
    public sealed class IndexerDef : Definition
    {
        public IndexerDef(eAccessLevel lvl, bool isProtected, bool isStatic, TypeNode type, List<VarDefinition> args)
            : base(lvl, isProtected, isStatic, type, args, null)
            => Kind = eNodeKind.Indexer;

        public override eNodeKind Kind { get; protected set; }
    }
}
