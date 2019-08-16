using Cordy.AST.Nodes.Definables;
using System;

namespace Cordy.AST
{
    public abstract class ExprVisitor : CompilerPart
    {
        protected ExprVisitor() { }

        public virtual BasicNode Visit(BasicNode node)
            => node.Accept(this);

        internal protected virtual BasicNode VisitExtension(BasicNode node)
            => node.VisitChildren(this);

        #region Expression Nodes

        internal protected virtual BasicNode VisitExpression(Expression node)
            => node;
        internal protected virtual BasicNode VisitCall(CallFunctionNode node)
            => node;
        internal protected virtual BasicNode VisitVariable(VarNode node)
             => node;
        internal protected virtual BasicNode VisitInteger(IntegerNode node)
            => node;
        internal protected virtual BasicNode VisitFloat(FloatNode node)
            => node;
        internal protected virtual BasicNode VisitType(TypeNode node)
            => node;
        [Obsolete("Not done")]
        internal protected virtual BasicNode VisitVariableDef(VarDefinition node)
            => node;

        #endregion

        #region Code Blocks


        #region Simple Blocks

        internal protected virtual BasicNode VisitCodeBlock(CodeBlock block)
            => block;
        internal protected virtual BasicNode VisitExprBlock(ExprBlock block)
            => block;

        #endregion

        internal protected virtual BasicNode VisitReturnBlock(ReturnBlock block)
            => block;

        #region Branching

        internal protected virtual BasicNode VisitIfBlock(IfBlock block)
            => block;
        internal protected virtual BasicNode VisitSwitchBlock(SwitchBlock block)
            => block;

        //TODO: Add goto
        //TODO: Add labels
        #endregion

        #region Loops

        internal protected virtual BasicNode VisitForBlock(ForBlock block)
            => block;
        internal protected virtual BasicNode VisitForeachBlock(ForeachBlock block)
            => block;
        internal protected virtual BasicNode VisitWhileBlock(WhileBlock block)
            => block;
        internal protected virtual BasicNode VisitDoWhileBlock(DoWhileBlock block)
            => block;

        #endregion

        #region Exception Handling

        internal protected virtual BasicNode VisitTryBlock(TryBlock block)
            => block;

        #endregion

        #endregion

        #region Type Members

        internal protected virtual BasicNode VisitPropertyDef(Property node)
            => node;
        internal protected virtual BasicNode VisitOperatorDef(Operator node)
             => node;
        internal protected virtual BasicNode VisitIndexerDef(Indexer node)
            => node;
        internal protected virtual BasicNode VisitConstructorDef(Constructor node)
            => node;
        internal protected virtual BasicNode VisitIndexerDef(Event node)
            => node;
        internal protected virtual BasicNode VisitFunctionDef(Function node)
            => node;
        internal protected virtual BasicNode VisitPrototype(Definition node)
            => node;

        #endregion

    }
}