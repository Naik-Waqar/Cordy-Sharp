using Cordy.AST;
using Cordy.Codegen;
using Llvm.NET;
using Llvm.NET.DebugInfo;
using Llvm.NET.Instructions;
using Llvm.NET.Interop;
using Llvm.NET.ObjectFile;
using Llvm.NET.Transforms;
using Llvm.NET.Types;
using Llvm.NET.Values;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cordy
{
    using IRBuilder = InstructionBuilder;
    using Module = BitcodeModule;

    internal static class Compiler
    {
        #region Assembly Structure Generation
        internal static Namespace ROOT { get; private set; }

        /// <summary>
        /// Initializes process of compilation
        /// </summary>
        /// <param name="dir"></param>
        internal static void Init(string dir)
        {
            try
            {
                using (Library.InitializeLLVM())
                {
                    Library.RegisterAll();
                    //Library.RegisterNative();
                    ROOT = GetSubspaces(dir, dir, new Namespace(dir, Path.GetDirectoryName(dir)));
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        /// <summary>
        /// Creates sub-Namespaces depending on directories
        /// </summary>
        /// <param name="root"></param>
        /// <param name="dir"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static Namespace GetSubspaces(string root, string dir, Namespace n)
        {
            var dirs = Directory.GetDirectories(dir, "!.*", SearchOption.TopDirectoryOnly);
            foreach (var d in dirs)
            {
                if (!d.StartsWith("_"))
                    n.Subspaces.Add(GetSubspaces(dir, d, new Namespace(d, root, Path.GetDirectoryName(d))));
                else
                    InsertFiles(n, d);
            }
            InsertFiles(n, dir);
            return n;
        }

        internal static void CreateObject(Context context) => throw new NotImplementedException();

        /// <summary>
        /// Initializes files
        /// </summary>
        /// <param name="n"></param>
        /// <param name="dir"></param>
        private static void InsertFiles(Namespace n, string dir)
        {
            Directory.GetFiles(dir, "*.co", SearchOption.AllDirectories).ToList().
                ForEach(file =>
                    {
                        n.Types.Add(new CordyType(file, n));
                    }
                );
        }

        #endregion

        public static void Build() => ROOT.Build();

        /// <summary>
        /// List of known modules
        /// </summary>
        public static List<Module> Modules { get; } = new List<Module>();

        public static Dictionary<string, TargetObjectFile> Imports { get; } = new Dictionary<string, TargetObjectFile>();

        #region Output

        #region Console Logging
        [DebuggerStepThrough]
        internal static void Message(string msg, string file = null, (int, int)? pos = null, string stage = null)
            => Log("Message", ConsoleColor.Gray, msg, file, pos, stage);

        [DebuggerStepThrough]
        internal static void Info(string msg, string file = null, (int, int)? pos = null, string stage = null)
            => Log("Info", ConsoleColor.Cyan, msg, file, pos, stage);

        [DebuggerStepThrough]
        internal static void Warn(string msg, string file = null, (int, int)? pos = null, string stage = null)
            => Log("Warning", ConsoleColor.Yellow, msg, file, pos, stage);

        [DebuggerStepThrough]
        internal static void Error(string msg, string file = null, (int, int)? pos = null, string stage = null)
            => Log("Error", ConsoleColor.Red, msg, file, pos, stage);


        private static void Log(string lvl, ConsoleColor color, string msg, string file = null, (int row, int col)? pos = null, string stage = null)
        {
            if (file != null)
                Console.Write($"[{file}]");
            if (pos != null)
                Console.Write($"[{pos?.row}:{pos?.col}]");
            if (stage != null)
                Console.Write($"[{stage}]");

            Console.ForegroundColor = color;
            Console.Write($"[{lvl}]");
            Console.ResetColor();
            Console.WriteLine($" {msg}");
        }
        #endregion

        #region Dumping

        public static void Dump(ITypeRef type)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[LLVM Dump type]\n");
            Console.ResetColor();
            Console.WriteLine(type.ToString());
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n[LLVM Dump end]\n");
            Console.ResetColor();
        }

        public static void Dump(Module module)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[LLVM Dump module]\n");
            Console.ResetColor();
            Console.WriteLine(module.WriteToString());
            Console.WriteLine("\n" + module.GetTypeByName("long")?.ToString());
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[LLVM Dump end]\n");
            Console.ResetColor();
        }

        public static void Dump(Value value)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[LLVM Dump value]\n");
            Console.ResetColor();
            Console.WriteLine(value.ToString());
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[LLVM Dump end]\n");
            Console.ResetColor();
        }

        #endregion

        #endregion

        #region Buidling

        internal static FunctionPassManager InitPassManager(Module m)
        {
            // Create a function pass manager for this engine
            var pm = new FunctionPassManager(m);

            // Set up the optimizer pipeline.  Start with registering info about how the
            // target lays out data structures.

            if (false)
            {
                pm.AddBasicAliasAnalysisPass()
                  .AddPromoteMemoryToRegisterPass()
                  .AddInstructionCombiningPass()
                  .AddReassociatePass()
                  .AddGVNPass()
                  .AddCFGSimplificationPass();
            }

            pm.Initialize();
            return pm;
        }



        internal static void Build(CordyType type, Context context)
        {
            var JIT = new JIT();
            var module = context.CreateBitcodeModule(type.Name, (SourceLanguage)0xA000, type.FilePath, "C#-based Cordy Compiler");
            Modules.Add(module);
            type.Module = module;

            var builder = new IRBuilder(context);

            var passManager = InitPassManager(module);

            //var listener = new Listener(passManager, new Visitor(module, builder, context, type.Name));
            var generator = new Generator(module, builder, context, JIT, type.Name);
            var lexer = new Lexer();
            lexer.Prepare(type); //tokenizing code
            using (var parser = new Parser(type, lexer))
            {
                parser.Parse();
            }

            var members = type.GetAllMembers();
            foreach (var member in members)
                generator.Emit(member);
            generator.Dispose();

            Dump(module);
        }

        #endregion

        #region

        internal static ITypeRef GetTypeByName(string name)
        {
            //TODO: Make search in imports
            //TODO: Make non-keyword search
            foreach (var m in Modules)
            {
                var meta = m.NamedMetadata.ToList();
                var t = GetTypeFromKeyword(m, meta, name);
                if (t != null)
                    return t;
            }
            return null;
        }

        private static ITypeRef GetTypeFromKeyword(Module m, List<NamedMDNode> meta, string name)
        {
            var opers = meta.Find(x => x.Name == "cordy.types.keywords")?.Operands?.ToList();
            if (opers == null)
                return null;

            foreach (var o in opers)
            {
                var parts = o.GetOperandString(0).Split(new[] { ',', ':', '(', ')', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts[0] != name)
                    continue;

                return (ITypeRef)typeof(Context).GetMethod(parts[1]).Invoke(m.Context, new object[] { uint.Parse(parts[2]) });
            }
            return null;
        }

        internal static ExprOperator GetOperator(string rep)
        {
            var oper = ROOT.FindOperator(rep);
            if (oper != null)
                return oper;

            foreach (var m in Modules) //TODO: Make search in imports
            {
                var meta = m.NamedMetadata.ToList();
                oper = GetOperatorFromModule(meta, "prefix", rep)
                        ?? GetOperatorFromModule(meta, "binary", rep)
                        ?? GetOperatorFromModule(meta, "postfix", rep);
                if (oper != null)
                    return oper;
            }

            return null;
        }

        private static ExprOperator GetOperatorFromModule(List<NamedMDNode> meta, string group, string rep)
        {
            var opers = meta.Find(x => x.Name == "cordy.operators." + group)?.Operands?.ToList();
            if (opers == null)
                return null;

            foreach (var o in opers)
            {
                var name = o.GetOperandString(0);
                if (name != rep)
                    continue;

                var odata = meta.Find(x => x.Name == name).Operands.ToList();
                return new ExprOperator(odata, group, rep);//operator info
            }
            return null;
        }

        #endregion
    }
}
