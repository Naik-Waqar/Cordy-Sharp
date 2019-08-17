using Cordy.Codegen;
using System.Collections.Generic;

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
        protected internal override BasicNode Accept(Visitor visitor)
            => visitor.VisitCodeBlock(this);
    }
}
