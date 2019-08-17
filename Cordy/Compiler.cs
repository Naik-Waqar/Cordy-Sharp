using Cordy.AST;
using Llvm.NET;
using Llvm.NET.DebugInfo;
using Llvm.NET.Instructions;
using Llvm.NET.Transforms;
using Llvm.NET.Types;
using Llvm.NET.Values;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Llvm.NET.Interop;

namespace Cordy
{
    using Module = BitcodeModule;
    using IRBuilder = InstructionBuilder;
    using DIBuilder = DebugInfoBuilder;

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
            using (Library.InitializeLLVM())
            {
                Library.RegisterNative();
                ROOT = GetSubspaces(dir, dir, new Namespace(dir, Path.GetDirectoryName(dir)));
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
            var dirs = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);
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

        #region Output

        #region Console Logging
        [DebuggerStepThrough]
        internal static void Message(string msg, string file, (int, int) pos, string stage)
            => Log("Message", ConsoleColor.Gray, msg, file, pos, stage);

        [DebuggerStepThrough]
        internal static void Info(string msg, string file, (int, int) pos, string stage)
            => Log("Info", ConsoleColor.Cyan, msg, file, pos, stage);

        [DebuggerStepThrough]
        internal static void Warn(string msg, string file, (int, int) pos, string stage)
            => Log("Warning", ConsoleColor.Yellow, msg, file, pos, stage);

        [DebuggerStepThrough]
        internal static void Error(string msg, string file, (int, int) pos, string stage)
            => Log("Error", ConsoleColor.Red, msg, file, pos, stage);

        [DebuggerStepThrough]
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
            Console.WriteLine("[LLVM Dump type]\n");
            Console.ResetColor();
            Console.WriteLine(type.ToString());
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[LLVM Dump end]");
            Console.ResetColor();
        }

        public static void Dump(Module module)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[LLVM Dump module]\n");
            Console.ResetColor();
            Console.WriteLine(module.WriteToString());
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[LLVM Dump end]");
            Console.ResetColor();
        }

        public static void Dump(Value value)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[LLVM Dump value]\n");
            Console.ResetColor();
            Console.WriteLine(value.ToString());
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[LLVM Dump end]");
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
            // LLVM.DisposeTargetData(LLVM.GetExecutionEngineTargetData(engine));


            // Provide basic AliasAnalysis support for GVN.
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
            var module = context.CreateBitcodeModule(type.FullName, (SourceLanguage)0xA000, type.FilePath, "C#-based Cordy Compiler");
            //module.Layout = JIT.TargetMachine.TargetData;
            var builder = new IRBuilder(context);

            var passManager = InitPassManager(module);

            var listener = new CodegenListener(passManager, new CodegenVisitor(module, builder, context, type.FullName));

            var lexer = new Lexer();
            lexer.Prepare(type); //tokenizing code
            var parser = new Parser(type, lexer, listener);

            parser.Parse();
            Dump(module);
        }

        #endregion

        private static List<Module> ModulesToCompile;

        #region

        internal static ITypeRef GetTypeByName(string name)
        {
            throw new NotImplementedException();
        }

        internal static Operator GetOperator(string value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
