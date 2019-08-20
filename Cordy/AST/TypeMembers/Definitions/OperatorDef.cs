using System.Collections.Generic;

namespace Cordy.AST
{
    public sealed class OperatorDef : FunctionDef
    {
        public OperatorDef(TypeNode type, List<VarDefinition> pattern, string name)
            : base(eAccessLevel.Public, false, true, type, name, pattern)
        {
            switch (Args.Count)
            {
                case 1:
                    MetaParts["Kind"] = "prefix";
                    break;
                case 2:
                    MetaParts["Kind"] = "binary";
                    break;
            }
            MetaParts["Representation"] = Name;
            MetaParts["Precedence"] = "0";
            MetaParts["Type"] = "F";
            MetaParts["Callee"] = Name;
            MetaParts["Modules"] = "Int"; //TODO: Make required module evaluation


        }



        #region Parameters

        public void Instruction(string[] args)
        {
            MetaParts["Type"] = "I";
            MetaParts["Callee"] = args[0];
            if (!MetaParts.ContainsKey("Args"))
                MetaParts["Args"] = "";
            var i = args.Length > 2 ? 2 : 1;

            if (args.Length > 2)
                MetaParts["Args"] += "," + args[1];

            for (; i < args.Length; i++)
                MetaParts["Args"] += args[i];
        }

        public void Precedence(string[] args)
            => MetaParts["Precedence"] = args[0];

        public void Binary(string[] args)
            => MetaParts["Kind"] = "binary";

        public void Prefix(string[] args)
            => MetaParts["Kind"] = "prefix";

        public void Postfix(string[] args)
            => MetaParts["Kind"] = "postfix";

        #endregion

    }
}
