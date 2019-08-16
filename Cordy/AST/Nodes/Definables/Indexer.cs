using System;
using System.Collections.Generic;
using System.Text;

namespace Cordy.AST
{
    [Obsolete("Indexers not done yet")]
    public class Indexer : DefinedNode
    {
        public Indexer(List<string> parameters, List<string> attributes) : base(parameters, attributes)
        {
        }

        public override Definition Definition { get; }
        public override eNodeKind Kind { get; protected set; }
    }
}
