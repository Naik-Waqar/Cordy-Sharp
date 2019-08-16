using LLVMSharp;
using System.Collections.Generic;
using System.IO;

namespace Cordy
{
    /// <summary>
    /// Used for generation of new code
    /// </summary>
    public class CordyType
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
        public List<List<Lexem>> Parameters { get; } = new List<List<Lexem>>();

        /// <summary>
        /// List of LLVM's attributes and parameters
        /// </summary>
        public List<List<Lexem>> Attributes { get; } = new List<List<Lexem>>();

        public string FullName => $"{Name}.co";

        public bool haveSignature { get; private set; }

        #region LLVM

        /// <summary>
        /// Reference to LLVM module
        /// </summary>
        public LLVMModuleRef Module { get; private set; }

        /// <summary>
        /// Reference to defined LLVM type
        /// </summary>
        public LLVMTypeRef LLVMType { get; private set; }

        /// <summary>
        /// Associated IRBuilder
        /// </summary>
        public LLVMBuilderRef IRBuilder { get; private set; }

        #endregion

        public List<Lexem> Includes { get; } = new List<Lexem>();

        public List<string> Renames { get; } = new List<string>();

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

        internal void Build(LLVMContextRef context)
        {
            Compiler.Build(this, context);
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