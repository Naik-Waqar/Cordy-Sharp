using System.Collections.Generic;

namespace Cordy.AST
{
    /// <summary>
    /// Used for any definable element like function or property
    /// </summary>
    public abstract class DefinedNode : BasicNode
    {
        protected DefinedNode(List<string> parameters, List<string> attributes)
        {
            Parameters = parameters;
            Attributes = attributes;
        }

        public List<string> Parameters { get; }
        public List<string> Attributes { get; }

        public abstract Definition Definition { get; }
    }
}