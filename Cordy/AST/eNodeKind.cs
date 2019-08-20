using System;

namespace Cordy.AST
{
    /// <summary>
    /// Type of node
    /// </summary>
    public enum eNodeKind
    {
        #region Operator calls

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

        #endregion

        #region Variables

        /// <summary>
        /// Access to variable
        /// </summary>
        Variable,

        /// <summary>
        /// Integer literal
        /// </summary>
        Integer,

        /// <summary>
        /// Float literal
        /// </summary>
        Float,

        #endregion

        /// <summary>
        /// Expression of any kind (unary, binary, etc)
        /// </summary>
        Expression,

        #region Flow Control

        /// <summary>
        /// Conditional construction (if-elif-else)
        /// </summary>
        Branch,
        /// <summary>
        /// For loop
        /// </summary>
        LoopFor,

        #endregion

        //Used in calls, definitions and declarations
        #region Type Members

        /// <summary>
        /// Event
        /// </summary>
        Event,

        /// <summary>
        /// Function
        /// </summary>
        Function,

        /// <summary>
        /// Property
        /// </summary>
        Property,

        /// <summary>
        /// Constructor
        /// </summary>
        Constructor,

        /// <summary>
        /// Indexer
        /// </summary>
        Indexer,

        /// <summary>
        /// Operator
        /// </summary>
        Operator,

        #endregion

    }
}
