using Cordy.AST;
using Cordy.Exceptions;
using Llvm.NET;
using Llvm.NET.DebugInfo;
using Llvm.NET.Instructions;
using Llvm.NET.Types;
using Llvm.NET.Values;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Cordy.Codegen
{
    using DIBuilder = DebugInfoBuilder;
    using IRBuilder = InstructionBuilder;
    using Module = BitcodeModule;

    public class Visitor : CompilerPart, IDisposable
    {
        [DebuggerStepThrough]
        public BasicNode Visit(BasicNode node)
            => node.Accept(this);

        [DebuggerStepThrough]
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
                case "assign" when node.Args.Count != 2:
                default: //TODO: Add 'other' type
                    throw new Exception("Wrong expression");

                case "prefix":
                    throw new NotImplementedException("Prefix operators");
                case "postfix":
                    throw new NotImplementedException("Postfix operators");
                case "assign":
                case "binary":
                    foreach (var arg in node.Args)
                        Visit(arg); // LHS ,RHS

                    var rhs = Stack.Pop();
                    var lhs = Stack.Pop();
                    switch (op.CalleeType)
                    {
                        case "I": //TODO: Make type matching
                            var args = new List<object>();
                            switch (op.Callee)
                            {
                                case "Compare":
                                    foreach (var p in op.Predicate)
                                        args.Add((Predicate)Enum.Parse(typeof(Predicate), p, true));
                                    args.Add(lhs);
                                    args.Add(rhs);

                                    n = (Value)typeof(IRBuilder).GetMethod(op.Callee, new[] { typeof(Predicate), typeof(Value), typeof(Value) })
                                                                .Invoke(IRBuilder, args.ToArray()); //TODO: Make argumented instructions
                                    break;

                                default:
                                    if (rhs.NativeType.IsPointer)
                                        rhs = IRBuilder.Load(rhs);
                                    n = (Value)typeof(IRBuilder).GetMethod(op.Callee).Invoke(IRBuilder, new[] { lhs, rhs }); //TODO: Make argumented instructions
                                    break;
                            }
                            break;
                        case "F": //TODO: Make dynamic type recognition
                            throw new NotImplementedException("Function calls from operators");
                        default:
                            if (op.Kind != "assign")
                                throw new Exception("Wrong operator callee type");

                            if (rhs.NativeType.IsPointer)
                                rhs = IRBuilder.Load(rhs);

                            IRBuilder.Store(rhs, lhs); // write value
                            n = IRBuilder.Load(rhs.NativeType, lhs).RegisterName(lhs.Name[0..lhs.Name.IndexOf('.')]+".get"); // load written value
                            break;
                    }
                    break;
            }
            Stack.Push(n);
            return node;
        }

        protected internal BasicNode VisitCall(CallFunctionNode node)
            => node;

        protected internal BasicNode VisitVariable(VarNode node)
        {
            if (GetDefinedVariable(node.Name) == null)
                throw new Exception($"Unable to find variable {node.Name}");
            return node;
        }

        //[DebuggerStepThrough]
        protected internal BasicNode VisitInteger(IntegerNode node)
        {
            Stack.Push(Context.CreateConstant(Context.Int32Type, node.Value, node.Signed));
            return node;
        }

        //[DebuggerStepThrough]
        protected internal BasicNode VisitFloat(FloatNode node)
        {
            Stack.Push(Context.CreateConstant(node.Value));
            return node;
        }

        protected internal BasicNode VisitType(TypeNode node)
            => node;

        [Obsolete("Not done")]
        protected internal BasicNode VisitVariableDef(VarDefinition node)
        {
            var var = GetOrAllocateVar(Compiler.GetTypeByName(node.Type.Name), node.Name);
            namedValues[Depth][node.Name] = var;
            Stack.Push(var);
            return node;
        }
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
            Depth = 0;

            Visit(node.Definition);
            var func = (IrFunction)Stack.Pop();
            IRBuilder.PositionAtEnd(func.AppendBasicBlock(""));

            var keys = namedValues[0].Keys.ToList();
            for (var i = 0; i < namedValues[0].Count; i++)
                namedValues[0][keys[i]] = GetOrAllocateVar(namedValues[Depth][keys[i]].NativeType, keys[i]);

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

            if (!func.Verify(out var err))
                Error(err);

            Stack.Push(func);
            return node;
        }

        #endregion

        private Value GetDefinedVariable(string name)
        {
            for (var i = Depth; i >= 0; i--)
            {
                if (namedValues[i].TryGetValue(name, out var value))
                {
                    Stack.Push(value);
                    return value;
                }
            }
            return null;
        }

        private Value GetOrAllocateVar(ITypeRef type, string name)
        {
            var v = GetDefinedVariable(name);
            if (v != null)
                if (v.GetType() == typeof(Alloca))
                    return namedValues[Depth][name];
                else
                {
                    var al = IRBuilder.Alloca(namedValues[Depth][name].NativeType).RegisterName(name + ".ptr");
                    IRBuilder.Store(namedValues[Depth][name], al);
                    return namedValues[Depth][name] = al;
                }

            return namedValues[Depth][name] = IRBuilder.Alloca(type).RegisterName(name + ".ptr");
        }

        #region Definition

        protected internal BasicNode VisitFunctionDefinition(FunctionDef node)
        {
            //TODO: Make TODOs
            var count = node.Args.Count;
            var args = new ITypeRef[count];

            var func = Module.GetFunction(node.Name);

            //TODO: remove this piece of shit (idk how)
            if (func != null)
            {
                if (func.BasicBlocks.Count != 0) //TODO: overload
                {
                    Error($"Member '{node.Name}' already defined");
                    return null;
                }
                if (func.Parameters.Count != count)
                {
                    Error($"Member '{node.Name}' already defined with another count of args"); //TODO: override
                    return null;
                }
            }

            for (var i = 0; i < count; i++)
            {
                if (node.Args[i].Type.Template?.Count != 0) //TODO: Make generics
                    Error($"Generic types aren't done yet. Result can differ from expectations");

                var t = Compiler.GetTypeByName(node.Args[i].Type.Name);
                if (t == null)
                {
                    Error($"Unknown type '{node.Args[i].Type.Name}'");
                    return null;
                }

                args[i] = t;
            }

            ITypeRef rtype;
            if (node.Type == null)
                rtype = Context.VoidType;
            else
                rtype = Compiler.GetTypeByName(node.Type?.Name);
            if (rtype == null)
                throw new exUnexpected($"unknown type {node.Type.Name}");

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
        private JIT Engine;

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
            Engine = new JIT();
        }

        public void ClearStack() => Stack.Clear();

        public void Dispose()
        {
            Engine.Dispose();
        }
    }
}
