namespace Cordy
{
    public abstract class CompilerPart
    {
        public abstract string Stage { get; }

        public abstract string FileName { get; }

        public abstract (int, int) Pos { get; }

        public virtual void Error(string msg)
            => Compiler.Error(msg, FileName, Pos, Stage);

        public virtual void Warn(string msg)
            => Compiler.Warn(msg, FileName, Pos, Stage);

        public virtual void Info(string msg)
            => Compiler.Info(msg, FileName, Pos, Stage);

        public virtual void Message(string msg)
            => Compiler.Message(msg, FileName, Pos, Stage);
    }
}
