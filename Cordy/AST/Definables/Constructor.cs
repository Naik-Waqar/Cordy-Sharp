using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    [Obsolete("Constructors not done yet")]
    public class Constructor : DefinedNode
    {
        public Constructor(List<string> parameters, List<string> attributes) : base(parameters, attributes)
        {
        }

        public override Definition Definition { get; }
        public override eNodeKind Kind { get; protected set; }
    }
}
