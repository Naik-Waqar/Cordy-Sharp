namespace Cordy.AST
{
    public interface iValue<T> : iValue
    {
        T Value { get; }
    }

    public interface iValue
    {

    }
}