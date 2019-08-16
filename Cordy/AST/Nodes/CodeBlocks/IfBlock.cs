using System;
using System.Collections.Generic;
using System.Text;

namespace Cordy.AST
{
    public sealed class IfBlock : CodeBlock
    {
        public IfBlock(int indent, Expression cond, CodeBlock then, CodeBlock elifs, CodeBlock els = null) 
            : base(new List<CodeBlock> { then, elifs, els }, indent)
        {
            Condition = cond;
        }

        //Then block is named as child!

        public Expression Condition { get; }

        public override eNodeKind Kind { get; protected set; }

        protected internal override BasicNode Accept(ExprVisitor visitor)
            => visitor.VisitIfBlock(this);
    }
}
