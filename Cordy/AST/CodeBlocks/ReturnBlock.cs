using Cordy.AST;
using Cordy.Codegen;
using System.Collections.Generic;

namespace Cordy
{
    public sealed class ReturnBlock : CodeBlock
    {
        public ReturnBlock(ExprNode expr, int indent)
            : base(new List<CodeBlock> { new ExprBlock(new List<ExprNode> { expr }, indent) }, indent)
        {
        }

        protected internal override BasicNode Accept(Visitor visitor)
            => visitor.VisitReturnBlock(this);
    }
}
