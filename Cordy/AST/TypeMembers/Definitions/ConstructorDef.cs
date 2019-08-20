using System.Collections.Generic;

namespace Cordy.AST
{
    public sealed class ConstructorDef : Definition
    {
        public ConstructorDef(eAccessLevel lvl, bool isProtected, bool isStatic, TypeNode type, List<VarDefinition> args)
            : base(lvl, isProtected, isStatic, type, args, null)
        { }
    }
}
