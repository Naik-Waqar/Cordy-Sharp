using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    [Obsolete("Not done")]
    public sealed class DoWhileBlock : CodeBlock
    {
        public DoWhileBlock(List<CodeBlock> childs, int indent) : base(childs, indent)
        {
        }
    }
}