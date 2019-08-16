using System.Collections.Generic;

namespace Cordy.AST
{
    /// <summary>
    /// Represents a function call
    /// </summary>
    public sealed class CallFunctionNode : ExprNode
    {
        public CallFunctionNode(string callee, List<ExprNode> args)
        {
            Callee = callee;
            Args = args;
            Kind = eNodeKind.CallFunction;
        }

        public override eNodeKind Kind { get; protected set; }

        public string Callee { get; }

        public List<ExprNode> Args { get; }

        protected internal override BasicNode Accept(ExprVisitor visitor)
            => visitor.VisitCall(this);
    }
}