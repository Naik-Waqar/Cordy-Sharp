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

        }



        public string Callee { get; }

        public List<ExprNode> Args { get; }

    }
}
