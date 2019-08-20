using Cordy.AST;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cordy.Exceptions
{
    public sealed class exElementNotFound : Exception
    {
        public exElementNotFound(int i) : base()
        {
            lexID = i;
        }

        public readonly int lexID;
    }
}
