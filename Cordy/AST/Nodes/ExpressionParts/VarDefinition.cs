namespace Cordy.AST
{
    public class VarDefinition : VarNode
    {
        public VarDefinition(string name, TypeNode type) : base(name)
        {
            Type = type;
        }

        public TypeNode Type { get; }
    }
}
