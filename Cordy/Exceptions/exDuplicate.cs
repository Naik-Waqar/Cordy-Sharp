using System;

namespace Cordy.Exceptions
{
    /// <summary>
    /// File ended
    /// </summary>
    public sealed class exDuplicate : Exception
    {
        public exDuplicate(string member, string type)
            : base($"{member} already defined in '{type}' type with this arguments") { }
    }
}
