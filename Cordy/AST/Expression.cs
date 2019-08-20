using System.Collections.Generic;

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
        }

        public ExprOperator Operator { get; }

        public List<ExprNode> Args { get; }

    }
}
