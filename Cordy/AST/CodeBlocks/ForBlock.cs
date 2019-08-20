using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    [Obsolete("Not done")]
    public sealed class ForBlock : CodeBlock
    {
        public ForBlock(List<CodeBlock> childs, int indent) : base(childs, indent)
        {
        }
    }
}