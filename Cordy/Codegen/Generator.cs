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
using System.Runtime.CompilerServices;

namespace Cordy.Codegen
{
    using DIBuilder = DebugInfoBuilder;
    using IRBuilder = InstructionBuilder;
    using Module = BitcodeModule;

    public sealed class Generator : CompilerPart, IDisposable
    {
        #region DebugInfo (Console)

        public override string Stage { get; } = "IR Generator";

        public override string FileName { get; }

        public override (int, int)? Pos { get; } = null;

        #endregion

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<Generator, BasicNode, BasicNode> em(BasicNode n)
        {
            try
            {
                return Emitters[n.GetType().Name];
            }
            catch
            {
                throw new NotImplementedException(n.GetType().Name);
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Emit(BasicNode n) 
            => em(n)(this, n);

        private static Dictionary<string, Func<Generator, BasicNode, BasicNode>> Emitters { get; }
            = new Dictionary<string, Func<Generator, BasicNode, BasicNode>>
            {
                { typeof(Expression).Name, Expression },
                { typeof(FloatNode).Name, Float },
                { typeof(IntegerNode).Name, Integer },
                { typeof(ReturnBlock).Name, ReturnBlock },
                { typeof(VarNode).Name, DefinedVariable },
                { typeof(VarDefinition).Name, VariableDefinition },
                { typeof(TypeNode).Name, Type },
                { typeof(CodeBlock).Name, CodeBlock },
                { typeof(ExprBlock).Name, ExprBlock },
                { typeof(Operator).Name, Operator },
                { typeof(Function).Name, Function },
                { typeof(OperatorDef).Name, OperatorDef },
                { typeof(FunctionDef).Name, FunctionDef },
            };

        #region Expression Nodes

        internal static BasicNode Expression(Generator g, BasicNode bn)
        {
            var node = (Expression)bn;
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
                        g.Emit(arg); // LHS ,RHS

                    var rhs = g.Stack.Pop();
                    var lhs = g.Stack.Pop();
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
                                                                .Invoke(g.IRBuilder, args.ToArray());
                                    break;

                                default:
                                    if (rhs.NativeType.IsPointer)
                                        rhs = g.IRBuilder.Load(rhs);
                                    n = (Value)typeof(IRBuilder).GetMethod(op.Callee).Invoke(g.IRBuilder, new[] { lhs, rhs });
                                    break;
                            }
                            break;
                        case "F": //TODO: Make dynamic type recognition
                            throw new NotImplementedException("Function calls from operators");
                        default:
                            if (op.Kind != "assign")
                                throw new Exception("Wrong operator callee type");

                            if (rhs.NativeType.IsPointer)
                                rhs = g.IRBuilder.Load(rhs);

                            g.IRBuilder.Store(rhs, lhs); // write value
                            n = g.IRBuilder.Load(rhs.NativeType, lhs).RegisterName(lhs.Name[0..lhs.Name.IndexOf('.')] + ".get"); // load written value
                            break;
                    }
                    break;
            }
            g.Stack.Push(n);
            return node;
        }

        internal BasicNode VisitCall(CallFunctionNode node)
           => throw new NotImplementedException("Function calls");

        internal static BasicNode DefinedVariable(Generator g, BasicNode n)
        {
            var node = (VarNode)n;
            if (g.GetDefinedVariable(node.Name) == null)
                throw new Exception($"Unable to find variable {node.Name}");
            return node;
        }

        [DebuggerStepThrough]
        internal static BasicNode Integer(Generator g, BasicNode n)
        {
            var node = (IntegerNode)n;
            g.Stack.Push(g.Context.CreateConstant(g.Context.Int32Type, node.Value, true));
            return node;
        }

        [DebuggerStepThrough]
        internal static BasicNode Float(Generator g, BasicNode n)
        {
            var node = (FloatNode)n;
            g.Stack.Push(g.Context.CreateConstant(node.Value));
            return node;
        }

        internal static BasicNode Type(Generator g, BasicNode node)
           => node;

        [Obsolete("Not done")]
        internal static BasicNode VariableDefinition(Generator g, BasicNode n)
        {
            var node = (VarDefinition)n;
            var var = g.GetOrAllocateVar(Compiler.GetTypeByName(node.Type.Name), node.Name);
            g.namedValues[g.Depth][node.Name] = var;
            g.Stack.Push(var);
            return node;
        }
        #endregion

        #region Code Blocks

        #region Simple Blocks

        internal static BasicNode CodeBlock(Generator g, BasicNode b)
        {
            var block = (CodeBlock)b;
            foreach (var c in block.Childs)
                g.Emit(c);
            return block;
        }

        internal static BasicNode ExprBlock(Generator g, BasicNode b)
        {
            var block = (ExprBlock)b;
            foreach (var exp in block.Expressions)
                g.Emit(exp);
            return block;
        }
        #endregion

        internal static BasicNode ReturnBlock(Generator g, BasicNode b)
        {
            var block = (ReturnBlock)b;
            g.Emit(block.Childs[0]);
            g.IRBuilder.Return(g.Stack.Pop());
            return block;
        }

        #region Branching

        internal BasicNode VisitIfBlock(IfBlock block)
           => throw new NotImplementedException("Branching (if-elif-else)");

        internal BasicNode VisitSwitchBlock(SwitchBlock block)
           => throw new NotImplementedException("Branching (switch)");

        //TODO: Add goto
        //TODO: Add labels
        #endregion

        #region Loops

        internal BasicNode VisitForBlock(ForBlock block)
           => block;

        internal BasicNode VisitForeachBlock(ForeachBlock block)
           => block;

        internal BasicNode VisitWhileBlock(WhileBlock block)
           => block;

        internal BasicNode VisitDoWhileBlock(DoWhileBlock block)
           => block;

        #endregion

        #region Exception Handling

        internal BasicNode VisitTryBlock(TryBlock block)
           => block;

        #endregion

        #endregion

        #region Type Members

        #region Declaration

        internal BasicNode VisitProperty(Property node)
           => node;

        internal static BasicNode Operator(Generator g, BasicNode n)
        {
            var node = (Operator)n;
            g.namedValues.Clear();
            g.Emit(node.Definition);

            var oper = (IrFunction)g.Stack.Pop();
            if (oper == null)
            {
                g.Stack.Push(null);
                return node; //we parsed instruction
            }

            try // trying to parse body of function
            {
                g.Emit(node.Body);
            }
            catch (Exception)
            {
                g.Stack.Pop();
                oper.EraseFromParent();
                throw;
            }

            oper.Verify(out var err);
            if (!string.IsNullOrEmpty(err))
            {
                g.Error(err);
            }
            g.Stack.Push(oper);
            return node;
        }

        internal BasicNode VisitIndexer(Indexer node)
           => node;

        internal BasicNode VisitConstructor(Constructor node)
           => node;

        internal BasicNode VisitIndexer(Event node)
           => node;

        internal static BasicNode Function(Generator g, BasicNode n)
        {
            var node = (Function)n;
            g.namedValues.Clear();
            g.Depth = 0;

            g.Emit(node.Definition);
            var func = (IrFunction)g.Stack.Pop();
            g.IRBuilder.PositionAtEnd(func.AppendBasicBlock(""));

            var keys = g.namedValues[0].Keys.ToList();
            for (var i = 0; i < g.namedValues[0].Count; i++)
                g.namedValues[0][keys[i]] = g.GetOrAllocateVar(g.namedValues[g.Depth][keys[i]].NativeType, keys[i]);

            try
            {
                g.Emit(node.Body);
            }
            catch (Exception)
            {
                g.Stack.Pop();
                func.EraseFromParent();
                throw;
            }

            if (!func.Verify(out var err))
                g.Error(err);

            g.Stack.Push(func);
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

        internal static BasicNode FunctionDef(Generator g, BasicNode n)
        {
            var node = (FunctionDef)n;
            //TODO: Make TODOs
            var count = node.Args.Count;
            var args = new ITypeRef[count];

            var func = g.Module.GetFunction(node.Name);

            //TODO: remove this piece of shit (idk how)
            if (func != null)
            {
                if (func.BasicBlocks.Count != 0) //TODO: overload
                {
                    g.Error($"Member '{node.Name}' already defined");
                    return null;
                }
                if (func.Parameters.Count != count)
                {
                    g.Error($"Member '{node.Name}' already defined with another count of args"); //TODO: override
                    return null;
                }
            }

            for (var i = 0; i < count; i++)
            {
                if (node.Args[i].Type.Template?.Count != 0) //TODO: Make generics
                    g.Error($"Generic types aren't done yet. Result can differ from expectations");

                var t = Compiler.GetTypeByName(node.Args[i].Type.Name);
                if (t == null)
                {
                    g.Error($"Unknown type '{node.Args[i].Type.Name}'");
                    return null;
                }

                args[i] = t;
            }

            ITypeRef rtype;
            if (node.Type == null)
                rtype = g.Context.VoidType;
            else
                rtype = Compiler.GetTypeByName(node.Type?.Name);
            if (rtype == null)
                throw new exUnexpected($"unknown type {node.Type.Name}");

            //TODO: Apply storage modifiers
            //TODO: Get return type
            //TODO: Create different types of definitions
            func = g.Module.AddFunction(node.Name, g.Context.GetFunctionType(rtype, args, false));

            for (var i = 0; i < node.Args.Count; i++)
            {
                var name = node.Args[i].Name;
                func.Parameters[i].Name = name;
                while (g.namedValues.Count <= g.Depth)
                    g.namedValues.Add(new Dictionary<string, Value>());
                g.namedValues[g.Depth][name] = func.Parameters[i];
            }

            g.Stack.Push(func);
            return node;
        }

        internal static BasicNode OperatorDef(Generator g, BasicNode n)
        {
            var node = (OperatorDef)n;
            var count = node.Args.Count;
            var args = new ITypeRef[count];

            var rep = node.MetaParts["Representation"];

            // register operator in module

            //saving representation of operator in operator list
            g.Module.AddNamedMetadataOperand("cordy.operators." + node.MetaParts["Kind"], g.Context.CreateMDNode(node.MetaParts["Representation"]));

            //saving info about operator in specified metadata node
            g.Module.AddNamedMetadataOperand(rep, g.Context.CreateMDNode(node.MetaParts["Precedence"]));
            g.Module.AddNamedMetadataOperand(rep, g.Context.CreateMDNode(node.MetaParts["Modules"]));
            g.Module.AddNamedMetadataOperand(rep, g.Context.CreateMDNode(node.MetaParts["Type"]));
            g.Module.AddNamedMetadataOperand(rep, g.Context.CreateMDNode(node.MetaParts["Callee"]));
            g.Module.AddNamedMetadataOperand(rep, g.Context.CreateMDNode(node.MetaParts["Args"]));

            if (node.MetaParts["Type"] != "F") // if not function -> we are done
            {
                g.Stack.Push(null);
                return node;
            }
            return FunctionDef(g, node);
        }

        #endregion

        #endregion

        private Module Module;
        private IRBuilder IRBuilder;
        private DIBuilder DIBuilder;
        private Context Context;
        private JIT Engine;

        #region Values

        private readonly List<Dictionary<string, Value>> namedValues = new List<Dictionary<string, Value>>();

        public Stack<Value> Stack { get; } = new Stack<Value>();

        private int Depth;

        #endregion

        public Generator(Module module, IRBuilder builder, Context context, JIT engine, string filename)
        {
            Context = context;
            Module = module;
            IRBuilder = builder;
            FileName = filename;
            Engine = engine;
        }

        public void ClearStack() => Stack.Clear();

        public void Dispose()
        {
            if (!Engine.IsDisposed)
                Engine.Dispose();
        }
    }
}
