using System;

namespace Cordy.Exceptions
{
    public sealed class exTooManySignatures : Exception
    {
        public exTooManySignatures() : base("One type can't contain more than one signature. Processing failed") { }
    }
}
