using System.Collections.Generic;

namespace Cordy.AST
{
    public abstract class FunctionalMember : DefinedNode
    {
        protected FunctionalMember(Definition def, BasicNode body)
            : base(def)
            => Body = body;

        public BasicNode Body { get; }

        public List<FunctionalMember> Overrides { get; } = new List<FunctionalMember>();

    }
}
