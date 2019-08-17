using System.Collections.Generic;

namespace Cordy.AST
{
    /// <summary>
    /// Represents a declared operator
    /// </summary>
    public class Operator : DefinedNode
    {
        public Operator(eNodeKind type, Definition def, BasicNode body, List<string> parameters, List<string> attributes) : base(parameters, attributes) => Kind = type;//pattern defined in def.Args//args of opreator is expression with declaration of 2 elements without default value// precedence can be set by parameter. By default it's zero

        /// <summary>
        /// Operator precedence that used for expression parsing
        /// <para>Can be changed by parameters</para>
        /// </summary>
        public int Precedence { get; private set; } = 0;

        public override eNodeKind Kind { get; protected set; }

        public override Definition Definition { get; }

        //protected internal override BasicNode Accept(ExprVisitor visitor)
        //    => visitor.VisitOperator(this);

        public void SetPrecedence(int val) => Precedence = val;
    }
}
