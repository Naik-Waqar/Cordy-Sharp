using Cordy.AST;
using Llvm.NET.Transforms;
using Llvm.NET.Values;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cordy.Codegen
{
    internal class Listener
    {
        /// <summary>
        /// Type of listener
        /// </summary>
        private static Type Type => typeof(Listener);

        private readonly Stack<string> descentStack = new Stack<string>();

        private readonly Stack<ASTContext> ascentStack = new Stack<ASTContext>();

        internal void EnterRule(string v)
            => descentStack.Push(v);

        internal void ExitRule(BasicNode arg)
        {
            var rule = descentStack.Pop();
            ascentStack.Push(new ASTContext(Type.GetMethod($"Exit{rule}"), this, arg));
            ascentStack.Push(new ASTContext(Type.GetMethod($"Enter{rule}"), this, arg));
        }

        internal void Listen()
        {
            while (ascentStack.Count > 0)
            {
                var context = ascentStack.Pop();
                context.MethodInfo.Invoke(context.Instance, new object[] { context.Arg });
            }
        }

        private FunctionPassManager FunctionPassManager;
        private Visitor Visitor;

        public Listener(FunctionPassManager passManager, Visitor visitor)
        {
            Visitor = visitor;
            FunctionPassManager = passManager;
        }

        public void EnterHandleFunctionDefinition(Function data)
        { }

        public void ExitHandleFunctionDefinition(Function data)
        {
            Visitor.Visit(data);
            var func = (IrFunction)Visitor.Stack.Pop();

            FunctionPassManager.Run(func);
        }


        private sealed class ASTContext
        {
            public ASTContext(MethodInfo methodInfo, object instance, BasicNode argument)
            {
                MethodInfo = methodInfo;
                Instance = instance;
                Arg = argument;
            }

            public MethodInfo MethodInfo { get; private set; }

            public BasicNode Arg { get; set; }

            public object Instance { get; private set; }
        }

    }
}
