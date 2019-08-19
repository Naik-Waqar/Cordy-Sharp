using Cordy.Codegen;
using System.Collections.Generic;

namespace Cordy.AST
{
    /// <summary>
    /// Represents a function definition
    /// </summary>
    public class FunctionDef : Definition
    {
        public FunctionDef(eAccessLevel lvl, bool isProtected, bool isStatic, TypeNode type, string name, List<VarDefinition> args)
            : base(lvl, isProtected, isStatic, type, args, name)
            => Kind = eNodeKind.Function;

        public override eNodeKind Kind { get; protected set; }

        protected internal override BasicNode Accept(Visitor visitor)
            => visitor.VisitFunctionDefinition(this);
    }
}