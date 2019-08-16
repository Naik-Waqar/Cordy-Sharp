using System.Collections.Generic;

namespace Cordy.AST
{
    public class Function : DefinedNode
    {
        public Function(Definition def, BasicNode body, List<string> parameters, List<string> attributes) : base(parameters, attributes)
        {
            Definition = def;
            Body = body;
            Kind = eNodeKind.Function;
        }

        public BasicNode Body { get; }

        public override eNodeKind Kind { get; protected set; }

        public override Definition Definition { get; }

        protected internal override BasicNode Accept(ExprVisitor visitor)
            => visitor.VisitFunctionDef(this);
    }
}