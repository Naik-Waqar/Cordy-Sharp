using System;
using System.Collections.Generic;
using System.Text;

namespace Cordy.AST
{
    public abstract class BasicNode
    {
        public abstract eNodeKind Kind { get; protected set; }

        internal protected virtual BasicNode VisitChildren(ExprVisitor visitor) 
            => visitor.Visit(this);

        internal protected virtual BasicNode Accept(ExprVisitor visitor) 
            => visitor.VisitExtension(this);
    }
}
