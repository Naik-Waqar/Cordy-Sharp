using System.Collections.Generic;

namespace Cordy.AST
{
    public sealed class EventDef : Definition
    {
        public EventDef(eAccessLevel lvl, bool isProtected, bool isStatic, List<VarDefinition> args, string name)
            : base(lvl, isProtected, isStatic, null, args, name)
            {}

        
    }
}
