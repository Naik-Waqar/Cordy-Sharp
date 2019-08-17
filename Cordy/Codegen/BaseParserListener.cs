using Cordy.AST;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cordy
{
    internal class BaseParserListener
    {
        /// <summary>
        /// Type of listener
        /// </summary>
        private static Type Type => typeof(iParserListener);

        /// <summary>
        /// Listener
        /// </summary>
        private readonly iParserListener Listener;

        private readonly Stack<string> descentStack = new Stack<string>();

        private readonly Stack<ASTContext> ascentStack = new Stack<ASTContext>();

        public BaseParserListener(iParserListener listener)
            => Listener = listener;

        internal void EnterRule(string v)
            => descentStack.Push(v);

        internal void ExitRule(BasicNode arg)
        {
            var rule = descentStack.Pop();
            ascentStack.Push(new ASTContext(Type.GetMethod($"Exit{rule}"), Listener, arg));
            ascentStack.Push(new ASTContext(Type.GetMethod($"Enter{rule}"), Listener, arg));
        }

        internal void Listen()
        {
            if (Listener != null)
                while (ascentStack.Count > 0)
                {
                    var context = ascentStack.Pop();
                    context.MethodInfo.Invoke(context.Instance, new object[] { context.Arg });
                }
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
