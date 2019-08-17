using Cordy.Codegen;

namespace Cordy.AST
{
    public abstract class BasicNode
    {
        public abstract eNodeKind Kind { get; protected set; }

        protected internal virtual BasicNode VisitChildren(Visitor visitor)
            => visitor.Visit(this);

        protected internal virtual BasicNode Accept(Visitor visitor)
            => visitor.VisitExtension(this);
    }
}
