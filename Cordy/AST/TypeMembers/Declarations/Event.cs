namespace Cordy.AST
{
    public sealed class Event : FunctionalMember
    {
        public Event(EventDef def, BasicNode body)
            : base(def, body)
            => Kind = eNodeKind.Event;

        public override eNodeKind Kind { get; protected set; }
    }
}
