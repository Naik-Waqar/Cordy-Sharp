namespace Cordy
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Compiler.Init(args[0]);
            Compiler.Build();
        }
    }
}

namespace System
{
    using Runtime.Serialization;
    [Obsolete("Undone")]
    public class NotImplementedException : Exception
    {
        public NotImplementedException() : base() { }

        public NotImplementedException(string message) : base($"{message} not implemented yet") { }

        public NotImplementedException(string message, Exception innerException) : base(message, innerException) { }

        protected NotImplementedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
