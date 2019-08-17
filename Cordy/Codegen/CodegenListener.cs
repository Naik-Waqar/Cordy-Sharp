using Cordy.AST;
using Llvm.NET.JIT;
using Llvm.NET.Transforms;
using Llvm.NET.Values;

namespace Cordy
{
    class CodegenListener : iParserListener
    {
        private IExecutionEngine Engine;
        private FunctionPassManager FunctionPassManager;
        private CodegenVisitor Visitor;

        public CodegenListener(FunctionPassManager passManager, CodegenVisitor codegenVisitor)
        {
            FunctionPassManager = passManager;
            Visitor = codegenVisitor;
        }

        public CodegenListener(IExecutionEngine engine, FunctionPassManager passManager, CodegenVisitor codegenVisitor)
        {
            Engine = engine;
            FunctionPassManager = passManager;
            Visitor = codegenVisitor;
        }

        public void EnterHandleFunctionDefinition(Function data)
        { }

        public void ExitHandleFunctionDefinition(Function data)
        {
            Visitor.Visit(data);
            var func = (IrFunction)Visitor.Stack.Pop();

            FunctionPassManager.Run(func);
        }
    }
}
