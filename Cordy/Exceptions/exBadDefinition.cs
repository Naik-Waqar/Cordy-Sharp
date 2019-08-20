using System;

namespace Cordy.Exceptions
{
    public sealed class exBadDefinition : Exception
    {
        public exBadDefinition(string msg) : base($"Bad definition: {msg}. Declaration skipped") { }
    }
}
