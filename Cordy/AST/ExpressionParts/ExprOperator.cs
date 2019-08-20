using Llvm.NET;
using System;
using System.Collections.Generic;

namespace Cordy.AST
{
    public sealed class ExprOperator
    {
        public ExprOperator(List<MDNode> data, string kind, string rep)
        {
            //TODO: Move operator data from strings
            Kind = kind;
            Representation = rep;

            Precedence = int.Parse(data[0].GetOperandString(0));
            RequiredModules = data[1].GetOperandString(0).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            CalleeType = data[2].GetOperandString(0);
            Callee = data[3].GetOperandString(0);
            Predicate = data[4].GetOperandString(0).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Used only for assignment operator
        /// </summary>
        public ExprOperator()
        {
            Kind = "assign";
        }

        public string Representation { get; }

        public string Kind { get; }

        public int Precedence { get; }

        public string[] RequiredModules { get; }

        public string CalleeType { get; }

        public string Callee { get; }

        public string[] Predicate { get; }
    }
}
