using System;
using System.Collections.Generic;
using System.Text;

namespace Cordy.AST
{
    public sealed class Property : DefinedNode
    {
        public Property(List<string> parameters, List<string> attributes) : base(parameters, attributes)
        {
        }
        //TODO: Make GET
        //TODO: Make SET

        public override Definition Definition { get; }

        public override eNodeKind Kind { get; protected set; }
    }
}
