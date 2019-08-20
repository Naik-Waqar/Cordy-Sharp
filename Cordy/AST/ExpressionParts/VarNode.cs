namespace Cordy.AST
{
    /// <summary>
    /// Represents a usage of property or variable
    /// </summary>
    public class VarNode : ExprNode
    {
        public VarNode(string name) => Name = name;

        public string Name { get; }

        public override string ToString() => Name;

    }
}
