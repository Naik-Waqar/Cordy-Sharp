using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    [Obsolete("Not done")]
    public sealed class ForeachBlock : CodeBlock
    {
        public ForeachBlock(List<CodeBlock> childs, int indent) : base(childs, indent)
        {
        }
    }
}