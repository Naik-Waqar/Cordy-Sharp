using System;

namespace Cordy
{
    public sealed partial class CordyType
    {
        public void TypeInt(string[] args)
        {
            var name = args[0];
            var size = Convert.ToUInt32(args[1]);
            var s = Module.Context.CreateMDNode($"{name}:GetIntType({size})");
            Module.AddNamedMetadataOperand("cordy.types.keywords", s);
        }
    }
}
