using System;

namespace Cordy.Exceptions
{
    public sealed class exBadDeclarationPos : Exception
    {
        public exBadDeclarationPos(string el) : base($"{el} can be defined only in class or interface. Skipping declaration") { }
    }
}
