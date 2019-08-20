using System.Collections.Generic;

namespace Cordy.AST
{
    public sealed class IfBlock : CodeBlock
    {
        public IfBlock(int indent, Expression cond, CodeBlock then, CodeBlock elifs, CodeBlock els = null)
            : base(new List<CodeBlock> { then, elifs, els }, indent) => Condition = cond;

        //Then block is named as child!

        public Expression Condition { get; }
    }
}
