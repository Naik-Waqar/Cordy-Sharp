using Cordy.AST;
using System;
using System.Collections.Generic;

namespace Cordy
{
    public sealed class UnparsedMember
    {
        /// <summary>
        /// Used for parser retry on search fails
        /// </summary>
        /// <param name="i">Index of first lexem in statement</param>
        /// <param name="def">Member where got the fail</param>
        /// <param name="id">Element required for parsing</param>
        public UnparsedMember(int i, Definition def, Type type, int id, List<string> attributes, List<string> parameters)
        {
            LexID = i;
            FailedDefinition = def;
            FailedType = type;
            FailedToken = id;
            Attributes = attributes;
            Parameters = parameters;
        }

        /// <summary>
        /// Index of first lexem in statement
        /// </summary>
        public int LexID { get; }

        /// <summary>
        /// Type member where required element wasn't found
        /// </summary>
        public Definition FailedDefinition { get; }

        /// <summary>
        /// ID of failed token
        /// </summary>
        public int FailedToken { get; }
        public List<string> Attributes { get; }
        public List<string> Parameters { get; }
        public Type FailedType { get; internal set; }
    }
}
