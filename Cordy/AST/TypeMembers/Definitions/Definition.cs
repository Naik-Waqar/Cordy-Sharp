using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    public abstract class Definition : BasicNode
    {
        protected Definition(eAccessLevel lvl, bool isProtected, bool isStatic, TypeNode type, List<VarDefinition> args, string name)
        {
            Type = type;
            AccessLevel = lvl;
            IsProtected = isProtected;
            IsStatic = isStatic;
            Args = args;
            Name = name;
        }

        /// <summary>
        /// Return type
        /// </summary>
        public TypeNode Type { get; }

        /// <summary>
        /// Access level
        /// </summary>
        public eAccessLevel AccessLevel { get; }

        /// <summary>
        /// Is marked as protected
        /// </summary>
        public bool IsProtected { get; }

        /// <summary>
        /// Is marked as sealed
        /// </summary>
        public bool IsStatic { get; }

        /// <summary>
        /// List of arguments
        /// </summary>
        public List<VarDefinition> Args { get; }

        /// <summary>
        /// Name of element
        /// </summary>
        public string Name { get; }

        public Dictionary<string, string> MetaParts = new Dictionary<string, string>();

        internal void ApplyParameters(List<string> parameters)
        {
            foreach (var p in parameters)
            {
                var parts = p.Split(new[] { ',', ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    GetType().GetMethod(parts[0], new[] { typeof(string[]) }).Invoke(this, new[] { parts[1..(parts.Length)] });
                }
                catch
                {
                    Compiler.Error($"Wrong parameter '{parts[0]}'", null, null, "Apply Parameter");
                }
            }
        }

        public override bool Equals(object obj)
        {
            var def = (Definition)obj;
            if (def.Type != Type || def.Args.Count != Args.Count)
                return false;

            for (var i = 0; i < Args.Count; i++)
            {
                if (Args[i].Type.Name != def.Args[i].Type.Name)
                    return false;
            }

            return true;
        }
    }
}