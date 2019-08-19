using Cordy.Codegen;

namespace Cordy.AST
{
    public class Function : FunctionalMember
    {

        public Function(FunctionDef def, BasicNode body)
            : base(def, body)
            => Kind = eNodeKind.Function;

        public override eNodeKind Kind { get; protected set; }

        protected internal override BasicNode Accept(Visitor visitor)
            => visitor.VisitFunction(this);
    }
}