using System;

namespace Cordy.AST
{
    /// <summary>
    /// Represents a float literal
    /// </summary>
    public class FloatNode : ExprNode
    {
        public FloatNode(string rep) => Value = Convert.ToDouble(rep.Replace(",", "."));

        public double Value { get; }
    }
}
