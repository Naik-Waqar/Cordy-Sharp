using System;
using System.Collections.Generic;
using System.Text;

namespace Cordy.AST
{
    public class CodeBlock : BasicNode
    {
        public CodeBlock(List<CodeBlock> childs, int indent)
        {
            Childs = childs;
            Indent = indent;
        }

        /// <summary>
        /// Blocks of code inside of current
        /// </summary>
        public List<CodeBlock> Childs { get; }

        public int Indent { get; }
        public override eNodeKind Kind { get; protected set; }
        protected internal override BasicNode Accept(ExprVisitor visitor)
            => visitor.VisitCodeBlock(this);
    }
}
