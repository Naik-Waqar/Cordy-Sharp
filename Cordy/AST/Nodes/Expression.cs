using System.Collections.Generic;

namespace Cordy.AST
{
    /// <summary>
    /// Represents any expression with operator
    /// </summary>
    public class Expression : ExprNode
    {
        public Expression(Operator oper, List<ExprNode> args)
        {
            Args = args;
            Operator = oper;
            Kind = eNodeKind.Expression;
        }

        public Operator Operator { get; }

        public List<ExprNode> Args { get; }

        public override eNodeKind Kind { get; protected set; }

        protected internal override BasicNode Accept(ExprVisitor visitor) 
            => visitor.VisitExpression(this);
    }
}