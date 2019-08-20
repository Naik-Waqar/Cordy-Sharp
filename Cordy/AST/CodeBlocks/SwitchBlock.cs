using System.Collections.Generic;

namespace Cordy.AST
{
    public sealed class SwitchBlock : CodeBlock
    {
        public SwitchBlock(List<CodeBlock> childs, int indent) : base(childs, indent)
        {
        }
    }
}