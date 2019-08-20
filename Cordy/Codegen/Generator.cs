using System;
using System.Collections.Generic;
using System.Text;
using Cordy.AST;

namespace Cordy.Codegen
{
    public sealed class Generator : CompilerPart
    {
        #region DebugInfo (Console)

        public override string Stage { get; } = "Generator";

        public override string FileName { get; }

        public override (int, int)? Pos { get; } = null;

        #endregion

        internal void Emit(DefinedNode m)
        {
            Emitters[m.GetType()](m);
        }

        private static Dictionary<Type, Func<BasicNode, BasicNode>> Emitters { get; }
            = new Dictionary<Type, Func<BasicNode, BasicNode>>
            {
                { typeof(Expression), VisitExpression }
            };

        public static BasicNode VisitExpression(BasicNode expr)
        {
            return expr;
        }



    }
}
