using Llvm.NET;
using Llvm.NET.ObjectFile;
using Llvm.NET.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace Cordy
{
    using AST;
    using Exceptions;

    /// <summary>
    /// Used for generation of new code
    /// </summary>
    public sealed partial class CordyType
    {
        /// <summary>
        /// Is this type Class, Interface or Enum
        /// </summary>
        public eTypeContext Context { get; internal set; }

        #region Access Information

        /// <summary>
        /// Name that used for access
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Describes from where can this type be accessed
        /// </summary>
        public eAccessLevel AccessLevel { get; internal set; }

        /// <summary>
        /// Does this type accessible only from it's childs
        /// </summary>
        public bool isProtected { get; internal set; }

        /// <summary>
        /// Does this type allows to create instances
        /// </summary>
        public bool isStatic { get; internal set; }

        /// <summary>
        /// Does this type allows to inherit another types
        /// </summary>
        public bool isSealed { get; internal set; }

        #endregion

        #region Debug Info
        /// <summary>
        /// Types, this inherited from
        /// </summary>
        public List<CordyType> Parents { get; } = new List<CordyType>();

        /// <summary>
        /// Namespace that contains current type
        /// </summary>
        public Namespace Namespace { get; }

        /// <summary>
        /// Path to associated file
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// List of compilation process modifiers
        /// </summary>
        public List<string> Parameters { get; } = new List<string>();

        /// <summary>
        /// List of LLVM's attributes and parameters
        /// </summary>
        public List<string> Attributes { get; } = new List<string>();

        public string FullName => $"{Name}.co";

        #endregion

        public bool haveSignature { get; private set; }

        #region LLVM

        /// <summary>
        /// Reference to LLVM module
        /// </summary>
        public BitcodeModule Module { get; internal set; }

        /// <summary>
        /// Reference to associated LLVM type
        /// </summary>
        public ITypeRef LLVMType { get; private set; }

        #endregion

        /// <summary>
        /// Stores all modules loaded by include deirctives
        /// </summary>
        public List<TargetObjectFile> LoadedIncludes { get; } = new List<TargetObjectFile>();

        /// <summary>
        /// Creates new instance of <see cref="CordyType"/> class
        /// </summary>
        /// <param name="f">File where type is defined</param>
        /// <param name="name">Short name of class</param>
        public CordyType(string path, Namespace n)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            FilePath = path;
            Namespace = n;
        }

        internal void Build(Context context) => Compiler.Build(this, context);

        internal void ApplyParameter(string p)
        {
            try
            {
                var parts = p.Split(new[] { ',', ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                var m = typeof(CordyType).GetMethod(parts[0], new[] { typeof(string[]) });
                m.Invoke(this, new[] { parts[1..parts.Length] });
            }
            catch (Exception e)
            {
                Compiler.Error(e.Message, FullName, null, "Apply Parameter");
            }
        }

        internal void ApplyAttribute(string v) => throw new NotImplementedException("Attributes");

        public Dictionary<string, List<DefinedNode>> Members { get; } = new Dictionary<string, List<DefinedNode>>();

        internal void AddMember(DefinedNode m)
        {
            var g = m.GetType().Name;
            if (!Members.TryGetValue(g, out var group))
                group = Members[g] = new List<DefinedNode>();
            var i = group.FindIndex(x => x.Definition.Name == m.Definition.Name);
            if (i == -1)
            {
                group.Add(m);
                return;
            }

            else if (!(m is FunctionalMember))
                return;

            var gr = group[i] as FunctionalMember;
            var ex = gr.Overrides.Find(x => x.Definition.Equals(m.Definition));
            if (ex != null)
                throw new exDuplicate($"{g} '{m.Definition.Name}'", Name);

            gr.Overrides.Add(m as FunctionalMember);
        }

        internal List<DefinedNode> GetAllMembers()
        {
            var members = new List<DefinedNode>();
            foreach (var group in Members)
            {
                foreach (var member in group.Value)
                {
                    if (member is FunctionalMember)
                    {
                        foreach (var ov in (member as FunctionalMember).Overrides)
                            members.Add(ov);
                    }
                    members.Add(member);
                }
            }
            return members;
        }
    }

    public enum eTypeContext
    {
        Interface,
        Class,
        Enum,
        Undefined
    }
}
