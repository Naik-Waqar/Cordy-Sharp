using Cordy.Codegen;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cordy.AST
{
    /// <summary>
    /// Represents any expression with operator
    /// </summary>
    public class Expression : ExprNode
    {
        public Expression(ExprOperator oper, List<ExprNode> args)
        {
            Args = args;
            Operator = oper;
            Kind = eNodeKind.Expression;
        }

        public ExprOperator Operator { get; }

        public List<ExprNode> Args { get; }

        public override eNodeKind Kind { get; protected set; }

        [DebuggerStepThrough]
        protected internal override BasicNode Accept(Visitor visitor)
            => visitor.VisitExpression(this);
    }
}
