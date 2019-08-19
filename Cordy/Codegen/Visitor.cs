using Cordy.AST;
using Llvm.NET;
using Llvm.NET.DebugInfo;
using Llvm.NET.Instructions;
using Llvm.NET.Types;
using Llvm.NET.Values;
using System;
using System.Collections.Generic;

namespace Cordy.Codegen
{
    using DIBuilder = DebugInfoBuilder;
    using IRBuilder = InstructionBuilder;
    using Module = BitcodeModule;

    public class Visitor : CompilerPart
    {
        public BasicNode Visit(BasicNode node)
            => node.Accept(this);

        protected internal BasicNode VisitExtension(BasicNode node)
            => node.VisitChildren(this);

        #region Expression Nodes


        protected internal BasicNode VisitExpression(Expression node)
        {
            var op = node.Operator;
            Value n;
            switch (op.Kind)
            {
                case "prefix" when node.Args.Count != 1:
                case "postfix" when node.Args.Count != 1:
                case "binary" when node.Args.Count != 2:
                default: //TODO: Add 'other' type
                    throw new Exception("Wrong expression");

                case "prefix":
                    throw new NotImplementedException("Prefix operators");
                case "postfix":
                    throw new NotImplementedException("Postfix operators");
                case "binary":
                    foreach (var arg in node.Args)
                        Visit(arg); // LHS ,RHS

                    var rhs = Stack.Pop();
                    var lhs = Stack.Pop();
                    switch (op.CalleeType)
                    {
                        case "I": //TODO: Make type matching
                            n = (Value)typeof(IRBuilder).GetMethod(op.Callee, new[] { typeof(Value), typeof(Value) }).Invoke(IRBuilder, new[] { lhs, rhs });
                            Stack.Push(n);
                            return node;
                        case "F": //TODO: Make dynamic type recognition
                            throw new NotImplementedException("Function calls from operators");
                        default:
                            throw new Exception("Wrong operator callee type");
                    }
            }
        }

        protected internal BasicNode VisitCall(CallFunctionNode node)
            => node;

        protected internal BasicNode VisitVariable(VarNode node)
        {
            for (var i = Depth; i >= 0; i++)
            {
                if (namedValues[i].TryGetValue(node.Name, out var value))
                {
                    Stack.Push(value);
                    return node;
                }
            }
            throw new Exception($"Unable to find variable {node.Name}");
        }


        protected internal BasicNode VisitInteger(IntegerNode node)
        {
            Stack.Push(Context.CreateConstant(Context.Int32Type, node.Value, node.Signed));
            return node;
        }

        protected internal BasicNode VisitFloat(FloatNode node)
        {
            Stack.Push(Context.CreateConstant(node.Value));
            return node;
        }
        protected internal BasicNode VisitType(TypeNode node)
            => node;


        [Obsolete("Not done")]
        protected internal BasicNode VisitVariableDef(VarDefinition node) => null;
        #endregion

        #region Code Blocks


        #region Simple Blocks

        protected internal BasicNode VisitCodeBlock(CodeBlock block)
        {
            foreach (var c in block.Childs)
                Visit(c);
            return block;
        }

        protected internal BasicNode VisitExprBlock(ExprBlock block)
        {
            foreach (var exp in block.Expressions)
                Visit(exp);
            return block;
        }
        #endregion

        protected internal BasicNode VisitReturnBlock(ReturnBlock block)
        {
            Visit(block.Childs[0]);
            IRBuilder.Return(Stack.Pop());
            return block;
        }

        #region Branching

        protected internal BasicNode VisitIfBlock(IfBlock block)
            => block;
        protected internal BasicNode VisitSwitchBlock(SwitchBlock block)
            => block;

        //TODO: Add goto
        //TODO: Add labels
        #endregion

        #region Loops


        protected internal BasicNode VisitForBlock(ForBlock block)

            => block;

        protected internal BasicNode VisitForeachBlock(ForeachBlock block)

            => block;

        protected internal BasicNode VisitWhileBlock(WhileBlock block)

            => block;

        protected internal BasicNode VisitDoWhileBlock(DoWhileBlock block)

            => block;

        #endregion

        #region Exception Handling


        protected internal BasicNode VisitTryBlock(TryBlock block)

            => block;

        #endregion

        #endregion

        #region Type Members

        #region Declaration

        protected internal BasicNode VisitProperty(Property node)
            => node;

        protected internal BasicNode VisitOperator(Operator node)
        {
            namedValues.Clear();
            Visit(node.Definition);

            var oper = (IrFunction)Stack.Pop();
            if (oper == null)
            {
                Stack.Push(null);
                return node; //we parsed instruction
            }

            try // trying to parse body of function
            {
                Visit(node.Body);
            }
            catch (Exception)
            {
                Stack.Pop();
                oper.EraseFromParent();
                throw;
            }

            oper.Verify(out var err);
            if (!string.IsNullOrEmpty(err))
            {
                Error(err);
            }
            Stack.Push(oper);
            return node;
        }

        protected internal BasicNode VisitIndexer(Indexer node)
            => node;

        protected internal BasicNode VisitConstructor(Constructor node)
            => node;

        protected internal BasicNode VisitIndexer(Event node)
            => node;

        protected internal BasicNode VisitFunction(Function node)
        {
            namedValues.Clear();
            Visit(node.Definition);

            var func = (IrFunction)Stack.Pop();
            IRBuilder.PositionAtEnd(func.AppendBasicBlock("entry"));

            try
            {
                Visit(node.Body);
            }
            catch (Exception)
            {
                Stack.Pop();
                func.EraseFromParent();
                throw;
            }

            func.Verify(out var err);
            if (!string.IsNullOrEmpty(err))
            {
                Error(err);
            }
            Stack.Push(func);
            return node;
        }

        #endregion

        #region Definition

        protected internal BasicNode VisitFunctionDefinition(FunctionDef node)
        {
            //TODO: Make some cleanup
            var count = node.Args.Count;
            var args = new ITypeRef[count];

            var func = Module.GetFunction(node.Name);
            if (func != null)
            {
                if (func.BasicBlocks.Count != 0) //TODO: overload
                {
                    Error($"Member '{node.Name}' already defined");
                    return null;
                }
                if (func.Parameters.Count != count)
                {
                    Error($"Member '{node.Name}' already defined with another count of args"); // override
                    return null;
                }
            }
            for (var i = 0; i < count; i++)
            {
                if (node.Args[i].Type.Template?.Count != 0)
                    Error($"Generic types aren't done yet. Result can differ from expectations");
                var t = Compiler.GetTypeByName(node.Args[i].Type.Name); //TODO: Make generics
                if (t == null)
                {
                    Error($"Unknown type '{node.Args[i].Type.Name}'");
                    return null;
                }
                args[i] = t;
            }
            //var rtype = Compiler.GetTypeByName(node.Type?.Name);
            var rtype = Context.Int32Type;
            //TODO: Apply storage modifiers
            //TODO: Get return type
            //TODO: Create different types of definitions
            func = Module.AddFunction(node.Name, Context.GetFunctionType(rtype, args, false));

            for (var i = 0; i < node.Args.Count; i++)
            {
                var name = node.Args[i].Name;
                func.Parameters[i].Name = name;
                while (namedValues.Count <= Depth)
                    namedValues.Add(new Dictionary<string, Value>());
                namedValues[Depth][name] = func.Parameters[i];
            }

            Stack.Push(func);
            return node;
        }

        protected internal BasicNode VisitOperatorDefinition(OperatorDef node)
        {
            var count = node.Args.Count;
            var args = new ITypeRef[count];

            var rep = node.MetaParts["Representation"];

            // register operator in module

            //saving representation of operator in operator list
            Module.AddNamedMetadataOperand("cordy.operators." + node.MetaParts["Kind"], Context.CreateMDNode(node.MetaParts["Representation"]));

            //saving info about operator in specified metadata node
            Module.AddNamedMetadataOperand(rep, Context.CreateMDNode(node.MetaParts["Precedence"]));
            Module.AddNamedMetadataOperand(rep, Context.CreateMDNode(node.MetaParts["Modules"]));
            Module.AddNamedMetadataOperand(rep, Context.CreateMDNode(node.MetaParts["Type"]));
            Module.AddNamedMetadataOperand(rep, Context.CreateMDNode(node.MetaParts["Callee"]));
            Module.AddNamedMetadataOperand(rep, Context.CreateMDNode(node.MetaParts["Args"]));

            if (node.MetaParts["Type"] != "F") // if not function -> we are done
            {
                Stack.Push(null);
                return node;
            }
            return VisitFunctionDefinition(node);
        }

        #endregion

        #endregion

        private Module Module;
        private IRBuilder IRBuilder;
        private DIBuilder DIBuilder;
        private Context Context;

        #region Logging
        public override string Stage { get; } = "CodeGen";

        public override string FileName { get; }

        public override (int, int)? Pos { get; } = null;

        #endregion

        #region Values

        private readonly List<Dictionary<string, Value>> namedValues = new List<Dictionary<string, Value>>();

        public Stack<Value> Stack { get; } = new Stack<Value>();

        private int Depth;

        #endregion

        public Visitor(Module module, IRBuilder builder, Context context, string filename)
        {
            Context = context;
            Module = module;
            IRBuilder = builder;
            FileName = filename;
        }

        public void ClearStack() => Stack.Clear();

    }
}
