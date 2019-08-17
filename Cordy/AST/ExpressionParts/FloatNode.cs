using Cordy.Codegen;
using System;

namespace Cordy.AST
{
    /// <summary>
    /// Represents a float literal
    /// </summary>
    public class FloatNode : ExprNode, iValue<double>
    {
        public FloatNode(string rep)
        {
            Value = Convert.ToDouble(rep.Replace(",", "."));
            Kind = eNodeKind.Float;
        }

        public double Value { get; }

        public override eNodeKind Kind { get; protected set; }

        protected internal override BasicNode Accept(Visitor visitor)
            => visitor.VisitFloat(this);

    }
}
