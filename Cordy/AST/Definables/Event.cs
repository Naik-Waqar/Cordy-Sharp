using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    [Obsolete("Events not done yet")]
    public sealed class Event : DefinedNode
    {
        public Event(List<string> parameters, List<string> attributes) : base(parameters, attributes)
        {

        }
        public override Definition Definition { get; }
        public override eNodeKind Kind { get; protected set; }
    }
}
