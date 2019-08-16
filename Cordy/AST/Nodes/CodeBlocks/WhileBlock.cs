using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    [Obsolete("Not done")]
    public sealed class WhileBlock : CodeBlock
    {
        public WhileBlock(List<CodeBlock> childs, int indent) : base(childs, indent)
        {
        }
    }
}