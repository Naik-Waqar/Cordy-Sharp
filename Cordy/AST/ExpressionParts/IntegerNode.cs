using Cordy.Codegen;
using System;

namespace Cordy.AST
{
    /// <summary>
    /// Represents an integer literal
    /// </summary>
    public sealed class IntegerNode : ExprNode, iValue<ulong>, iSignedValue
    {
        public IntegerNode(string rep)
        {
            Signed = rep.StartsWith('-');
            if (Signed)
                rep.TrimStart('-');
            Value = Convert.ToUInt64(rep.Replace("_", "").ToLower(), rep[0] switch
            {
                'b' => 2,
                'o' => 8,
                'x' => 16,
                _ => 10,
            }) | (Signed ? (ulong)1 << 63 : 0);
            Kind = eNodeKind.Integer;
        }

        public ulong Value { get; }

        public bool Signed { get; }

        public override eNodeKind Kind { get; protected set; }

        protected internal override BasicNode Accept(Visitor visitor)
            => visitor.VisitInteger(this);

        public override string ToString()
            => Value.ToString();
    }
}
