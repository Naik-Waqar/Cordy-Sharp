using System;

namespace Cordy.AST
{
    /// <summary>
    /// Represents an integer literal
    /// </summary>
    public sealed class IntegerNode : ExprNode
    {
        public IntegerNode(string rep)
        {
            var signed = rep.StartsWith('-');
            if (signed)
                rep.TrimStart('-');
            Value = Convert.ToUInt64(rep.Replace("_", "").ToLower(), rep[0] switch
            {
                'b' => 2,
                'o' => 8,
                'x' => 16,
                _ => 10,
            }) | (signed ? (ulong)1 << 63 : 0);

        }

        public ulong Value { get; }

        public override string ToString()
            => Value.ToString();
    }
}
