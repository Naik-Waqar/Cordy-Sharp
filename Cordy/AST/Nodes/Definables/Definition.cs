using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    /// <summary>
    /// Represents a function, constructor, indexer or event definition
    /// </summary>
    [Obsolete("Not done")]
    public sealed class Definition : BasicNode
    {
        public Definition(eAccessLevel lvl, bool isProtected, bool isStatic, TypeNode type, string name, List<VarDefinition> args)
        {
            Type = type;
            Name = name;
            Args = args;
            AccessLevel = lvl;
            IsProtected = isProtected;
            IsStatic = isStatic;
            Kind = eNodeKind.Definition;
        }

        public TypeNode Type { get; }

        public string Name { get; }

        public List<VarDefinition> Args { get; }

        public eAccessLevel AccessLevel { get; }

        public bool IsProtected { get; }

        public bool IsStatic { get; }

        public override eNodeKind Kind { get; protected set; }

        protected internal override BasicNode Accept(ExprVisitor visitor)
            => visitor.VisitPrototype(this);
    }
}