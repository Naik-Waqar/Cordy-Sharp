using System;

namespace Cordy.Exceptions
{
    public sealed class exUnexpected : Exception
    {
        public exUnexpected(string tok) : base($"Unexpected token '{tok}'") { }
    }
}
