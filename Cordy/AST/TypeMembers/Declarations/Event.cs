﻿namespace Cordy.AST
{
    public sealed class Event : FunctionalMember
    {
        public Event(EventDef def, BasicNode body)
            : base(def, body)
        { }

        public DefinedNode Empty { get; protected set; } = null;
    }
}
