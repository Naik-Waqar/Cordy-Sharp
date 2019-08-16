using Cordy.AST;
using LLVMSharp;
using System;
using System.Collections.Generic;

namespace Cordy
{
    internal class CodegenVisitor : ExprVisitor
    {

        private LLVMModuleRef Module;
        private LLVMBuilderRef Builder;
        private LLVMContextRef Context;

        #region Logging
        public override string Stage { get; } = "CodeGen";
        public override string FileName { get; }
        public override (int, int) Pos { get; } = (0, 0);

        #endregion

        #region Constants

        private static readonly LLVMBool False = new LLVMBool(0);
        private static readonly LLVMValueRef nil = new LLVMValueRef(IntPtr.Zero);

        #endregion

        #region Values

        private readonly List<Dictionary<string, LLVMValueRef>> namedValues = new List<Dictionary<string, LLVMValueRef>>();

        public Stack<LLVMValueRef> Stack { get; } = new Stack<LLVMValueRef>();

        private int Depth;

        #endregion

        public CodegenVisitor(LLVMModuleRef module, LLVMBuilderRef builder, LLVMContextRef context, string filename)
        {
            Context = context;
            Module = module;
            Builder = builder;
            FileName = filename;
        }

        public void ClearStack() => Stack.Clear();

        protected internal override BasicNode VisitInteger(IntegerNode node)
        {
            Stack.Push(LLVM.ConstInt(LLVM.Int64Type(), node.Value, node.Signed));
            return node;
        }

        protected internal override BasicNode VisitFloat(FloatNode node)
        {
            Stack.Push(LLVM.ConstReal(LLVM.DoubleType(), node.Value));
            return node;
        }

        protected internal override BasicNode VisitReturnBlock(ReturnBlock block)
        {
            Visit(block.Childs[0]);
            LLVM.BuildRet(Builder, Stack.Pop());
            return block;
        }

        protected internal override BasicNode VisitCodeBlock(CodeBlock block)
        {
            foreach (var c in block.Childs)
                Visit(c);
            return block;
        }

        protected internal override BasicNode VisitExprBlock(ExprBlock block)
        {
            foreach (var exp in block.Expressions)
                Visit(exp);
            return block;
        }

        protected internal override BasicNode VisitPrototype(Definition node)
        {
            var count = node.Args.Count;
            var args = new LLVMTypeRef[Math.Max(count, 1)];
            var func = LLVM.GetNamedFunction(Module, node.Name);

            if (func.Pointer != IntPtr.Zero)
            {
                if (LLVM.CountBasicBlocks(func) != 0)
                {
                    Error($"Member '{node.Name}' redefined");
                    return null;
                }
                if (LLVM.CountParams(func) != count)
                {
                    Error($"Member '{node.Name}' redefined with another count of args");
                }
            }
            for (var i = 0; i < count; i++)
            {
                if (node.Args[i].Type.Template?.Count != 0)
                    Error($"Generic types aren't done yet. Result can differ from expectations");
                var t = Compiler.GetTypeByName(Module, node.Args[i].Type.Name);
                if (t.Pointer == IntPtr.Zero)
                {
                    Error($"Unknown type '{node.Args[i].Type.Name}'");
                    return null;
                }
                args[i + 1] = t;
            }
            args[0] = LLVM.Int32TypeInContext(Context);
            //TODO: Apply storage modifiers
            //TODO: Get return type
            //TODO: Create different types of definitions
            func = LLVM.AddFunction(Module, node.Name, LLVM.FunctionType(LLVM.Int32Type(), args, False));
            LLVM.SetLinkage(func, LLVMLinkage.LLVMExternalWeakLinkage);

            for (var i = 0; i < node.Args.Count; i++)
            {
                var name = node.Args[i].Name;
                var p = LLVM.GetParam(func, (uint)i);
                LLVM.SetValueName(p, name);
                namedValues[Depth][name] = p;
            }

            Stack.Push(func);
            return node;
        }

        protected internal override BasicNode VisitFunctionDef(Function node)
        {
            namedValues.Clear();
            Visit(node.Definition);

            var func = Stack.Pop();

            LLVM.PositionBuilderAtEnd(Builder, LLVM.AppendBasicBlock(func, "entry"));

            try
            {
                Visit(node.Body);
            }
            catch (Exception)
            {
                Stack.Pop();
                LLVM.DeleteFunction(func);
                throw;
            }

            LLVM.VerifyFunction(func, LLVMVerifierFailureAction.LLVMPrintMessageAction);
            Console.WriteLine("\n");
            Stack.Push(func);
            return node;
        }

        [Obsolete("Not done")]
        protected internal override BasicNode VisitVariableDef(VarDefinition node)
        {
            return null;
        }

        protected internal override BasicNode VisitVariable(VarNode node)
        {
            LLVMValueRef value;
            for (var i = Depth; i >= 0; i++)
            {
                if (namedValues[i].TryGetValue(node.Name, out value))
                {
                    Stack.Push(value);
                    return node;
                }
            }
            throw new Exception($"Unable to find variable {node.Name}");
        }

        protected internal override BasicNode VisitExpression(Expression node)
        {
            //if (node.Args.Count > 2 || node.Args.Count < 1)
            //    throw new Exception("Wrong expression");
            //
            //LLVMValueRef n;
            //
            //switch (node.Args.Count)
            //{
            //    case 1:
            //
            //    case 2:
            //        Visit(node.Args[0]);
            //        var l = Stack.Pop();
            //
            //        Visit(node.Args[1]);
            //        var r = Stack.Pop();
            //
            //        if (node.Operator is PredefinedOperator)
            //        {
            //            //n = RunPredefinedOperator(l, r, (PredefinedOperator)node.Operator);
            //            Stack.Push(n);
            //            return node;
            //            //MethodInfo
            //        }
            //
            //}
            return null;
        }

    }
}