using Cordy.AST;
using LLVMSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cordy
{
    internal static class Compiler
    {
        internal static Namespace ROOT { get; private set; }

        /// <summary>
        /// Initializes process of compilation
        /// </summary>
        /// <param name="dir"></param>
        internal static void Init(string dir)
        {
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetMC();

            ROOT = GetSubspaces(dir, dir, new Namespace(dir, Path.GetDirectoryName(dir)));
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

        public static void Build() => ROOT.Build();

        #region Output

        [DebuggerStepThrough]
        internal static void Message(string msg, string file, (int, int) pos, string stage)
            => Log("Message", ConsoleColor.Gray, msg, file, pos, stage);

        [DebuggerStepThrough]
        internal static void Info(string msg, string file, (int, int) pos, string stage)
            => Log("Info", ConsoleColor.Cyan, msg, file, pos, stage);

        [DebuggerStepThrough]
        internal static void Warn(string msg, string file, (int, int) pos, string stage)
            => Log("Warning", ConsoleColor.Yellow, msg, file, pos, stage);
        internal static LLVMTypeRef GetTypeByName(LLVMModuleRef module, string name) => throw new NotImplementedException();
        [DebuggerStepThrough]
        internal static void Error(string msg, string file, (int, int) pos, string stage)
            => Log("Error", ConsoleColor.Red, msg, file, pos, stage);



        [DebuggerStepThrough]
        private static void Log(string lvl, ConsoleColor color, string msg, string file = null, (int row, int col)? pos = null, string stage = null)
        {
            //Console.Write($"[{file}][{pos.row}:{pos.col}][{stage}] ");
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


        [DebuggerStepThrough]
        public static void Log(string stage = null, string msg = null)
        {
            if (stage != null)
                Console.Write($"[{stage}] ");
            if (msg != null)
                Console.WriteLine($" {msg}");
        }

        public static void Dump(LLVMTypeRef type)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[LLVM Dump type]\n");
            Console.ResetColor();
            LLVM.DumpType(type);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[LLVM Dump end]");
            Console.ResetColor();
        }

        public static void Dump(LLVMModuleRef module)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[LLVM Dump module]\n");
            Console.ResetColor();
            LLVM.DumpModule(module);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[LLVM Dump end]");
            Console.ResetColor();
        }

        public static void Dump(LLVMValueRef value)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[LLVM Dump value]\n");
            Console.ResetColor();
            LLVM.DumpValue(value);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[LLVM Dump end]");
            Console.ResetColor();
        }

        #endregion

        internal static LLVMPassManagerRef InitPassManager(LLVMModuleRef m)
        {
            // Create a function pass manager for this engine
            var pm = LLVM.CreateFunctionPassManagerForModule(m);

            // Set up the optimizer pipeline.  Start with registering info about how the
            // target lays out data structures.
            // LLVM.DisposeTargetData(LLVM.GetExecutionEngineTargetData(engine));

            // Provide basic AliasAnalysis support for GVN.
            LLVM.AddBasicAliasAnalysisPass(pm);

            // Promote allocas to registers.
            LLVM.AddPromoteMemoryToRegisterPass(pm);

            // Do simple "peephole" optimizations and bit-twiddling optimisations.
            LLVM.AddInstructionCombiningPass(pm);

            // Reassociate expressions.
            LLVM.AddReassociatePass(pm);

            // Eliminate Common SubExpressions.
            LLVM.AddGVNPass(pm);

            // Simplify the control flow graph (deleting unreachable blocks, etc).
            LLVM.AddCFGSimplificationPass(pm);

            LLVM.InitializeFunctionPassManager(pm);

            return pm;
        }

        internal static void Build(CordyType type, LLVMContextRef context)
        {
            var module = LLVM.ModuleCreateWithNameInContext(type.FullName, context);
            var builder = LLVM.CreateBuilderInContext(context);

            if (LLVM.CreateExecutionEngineForModule(out var engine, module, out var errorMessage).Value == 1)
            {
                Error(errorMessage, type.Name, (-1, -1), "LLVM Init");
                return;
            }

            var passManager = InitPassManager(module);

            var listener = new CodegenListener(engine, passManager, new CodegenVisitor(module, builder, context, type.FullName));

            var lexer = new Lexer();
            lexer.Prepare(type); //tokenizing code
            var parser = new Parser(type, lexer, listener);


            parser.Parse();
            Dump(module);
        }

        internal static Operator GetOperator(string value)
        {
            throw new NotImplementedException();
        }
    }
}
