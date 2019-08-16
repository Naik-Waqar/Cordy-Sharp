namespace Cordy.AST
{
    /// <summary>
    /// Type of node
    /// </summary>
    public enum eNodeKind
    {
        /// <summary>
        /// Unary prefix operator
        /// </summary>
        OperatorUnaryPrefix,
        /// <summary>
        /// Unary postfix operator
        /// </summary>
        OperatorUnaryPostfix,
        /// <summary>
        /// Binary operator
        /// </summary>
        OperatorBinary,
        /// <summary>
        /// Function call
        /// </summary>
        CallFunction,
        /// <summary>
        /// Indexer call
        /// </summary>
        CallIndexer,
        /// <summary>
        /// Constructor call
        /// </summary>
        CallConstructor,
        /// <summary>
        /// Event call
        /// </summary>
        CallEvent,
        /// <summary>
        /// Access to variable
        /// </summary>
        Variable,
        /// <summary>
        /// Declared event
        /// </summary>
        Event,
        /// <summary>
        /// Declared function
        /// </summary>
        Function,
        /// <summary>
        /// Declared property
        /// </summary>
        Property,
        /// <summary>
        /// Declared indexer
        /// </summary>
        Indexer,
        /// <summary>
        /// Declared constructor
        /// </summary>
        Constructor,
        /// <summary>
        /// Integer literal
        /// </summary>
        Integer,
        /// <summary>
        /// Float literal
        /// </summary>
        Float,
        /// <summary>
        /// Conditional construction (if-elif-else)
        /// </summary>
        Branch,
        /// <summary>
        /// For loop
        /// </summary>
        LoopFor,
        /// <summary>
        /// Expression of any kind (unary, binary, etc)
        /// </summary>
        Expression,
        /// <summary>
        /// Definition of any definable element like function or property
        /// </summary>
        Definition
    }
}