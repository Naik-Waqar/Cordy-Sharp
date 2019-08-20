using System.Collections.Generic;

namespace Cordy.AST
{
    /// <summary>
    /// Represents a reference to type
    /// </summary>
    public class TypeNode : BasicNode
    {
        public TypeNode(string name, List<ExprNode> settings, List<TypeNode> template)
        {
            Name = name;
            Template = template;
            Settings = settings;
        }

        public string Name { get; }

        public List<TypeNode> Template { get; }

        public List<ExprNode> Settings { get; }

        public override string ToString()
        {
            var s = Name;
            foreach (var set in Settings)
                s += $":{set}";
            if (Template.Count > 0)
            {
                s += $"{{{Template[0]}";
                for (var i = 1; i < Template.Count; i++)
                {
                    s += $",{Template[i]}";
                }
                //s.Remove(s.Length - 2);
                s += "}";
            }
            return s;
        }
    }
}
