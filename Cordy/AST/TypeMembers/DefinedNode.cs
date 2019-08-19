using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    /// <summary>
    /// Used for any definable element like function or property
    /// </summary>
    public abstract class DefinedNode : BasicNode
    {
        protected DefinedNode(Definition def)
            => Definition = def;

        public Definition Definition { get; }

    }
}