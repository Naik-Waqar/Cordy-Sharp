using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    [Obsolete("Not done")]
    public class TryBlock : CodeBlock
    {
        public TryBlock(List<CodeBlock> childs, int indent) : base(childs, indent)
        {
        }
    }
}