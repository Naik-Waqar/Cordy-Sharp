using System.Collections.Generic;

namespace Cordy
{
    public sealed class PreParsedDef
    {
        /// <summary>
        /// List of compilation process modifiers that will be applied on declaration parsing process
        /// </summary>
        public List<List<Lexem>> Parameters = new List<List<Lexem>>();

        /// <summary>
        /// List of LLVM's attributes and parameters
        /// </summary>
        public List<List<Lexem>> Attributes = new List<List<Lexem>>();

        public eAccessLevel AccesModifier;

        public bool isStatic;

        public bool isProtected;

        public string Name;

        public string Type;

        public eDefinitionType Kind;

        public List<Lexem> Args;

        /// <summary>
        /// Start of body
        /// </summary>
        public int Start;

        /// <summary>
        /// End of body
        /// </summary>
        public int Length;
    }

    public enum eDefinitionType
    {
        Undefined,
        Function,
        Operator,
        Property,
        Indexer,
        Constructor,
        Event
    }
}
