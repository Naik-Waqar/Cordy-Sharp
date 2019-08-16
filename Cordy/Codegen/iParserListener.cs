namespace Cordy
{
    using AST;

    public interface iParserListener
    {
        void EnterHandleFunctionDefinition(Function data);

        void ExitHandleFunctionDefinition(Function data);
    }
}