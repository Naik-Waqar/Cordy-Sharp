using Cordy.Codegen;

namespace Cordy.AST
{
    /// <summary>
    /// Represents a usage of property or variable
    /// </summary>
    public class VarNode : ExprNode, iValue
    {
        public VarNode(string name)
        {
            Name = name;
            Kind = eNodeKind.Variable;
        }

        public string Name { get; }

        public override eNodeKind Kind { get; protected set; }

        protected internal override BasicNode Accept(Visitor visitor)
            => visitor.VisitVariable(this);

        public override string ToString() => Name;

    }
}
