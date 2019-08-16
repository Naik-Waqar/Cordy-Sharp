using Cordy.AST;
using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cordy
{
    class CodegenListener : iParserListener
    {
        private LLVMExecutionEngineRef Engine;
        private LLVMPassManagerRef Manager;
        private CodegenVisitor Visitor;

        public CodegenListener(LLVMExecutionEngineRef engine, LLVMPassManagerRef passManager, CodegenVisitor codegenVisitor)
        {
            Engine = engine;
            Manager = passManager;
            Visitor = codegenVisitor;
        }

        public void EnterHandleFunctionDefinition(Function data)
        { }

        public void ExitHandleFunctionDefinition(Function data)
        {
            Visitor.Visit(data);
            var func = Visitor.Stack.Pop();

            LLVM.RunFunctionPassManager(Manager, func);
        }
    }
}
