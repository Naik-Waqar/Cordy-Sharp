using Llvm.NET;
using System.Collections.Generic;
using System.IO;

namespace Cordy
{
    public class Namespace
    {
        public Namespace(string path, string name)
        {
            Name = name.Replace(' ', '_');
            FullName = new[] { Name };
            Directory = path;
        }

        public Namespace(string path, string root, string name) : this(path, name) => FullName = Path.GetRelativePath(root, path).Replace(' ', '_').Split(new[] { '\\', '/' }, System.StringSplitOptions.RemoveEmptyEntries);

        public string Directory { get; }

        public string Name { get; }

        public string[] FullName { get; }

        public List<Namespace> Subspaces { get; } = new List<Namespace>();

        public List<CordyType> Types { get; } = new List<CordyType>();

        internal string GetFullName()
        {
            var name = FullName[0];

            for (var i = 1; i < FullName.Length; i++)
            {
                var p = FullName[i];
                name += '.' + p;
            }

            return name;
        }

        /// <summary>
        /// Compiles all the code
        /// </summary>
        internal void Build()
        {
            var context = new Context();
            for (var i = 0; i < Subspaces.Count; i++)
                Subspaces[i].Build();

            for (var i = 0; i < Types.Count; i++)
            {
                Types[i].Build(context);
            }
            //Compiler.CreateObject(context);
            context.Dispose();
        }
    }
}
