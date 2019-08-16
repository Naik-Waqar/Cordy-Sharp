using Cordy.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cordy
{
    internal partial class CordyParser
    {
        //private static Dictionary<string, int> OperatorPrecedences = new Dictionary<string, int>
        //{
        //    { "bitcast", 20 },
        //    { "fneg", 20 },
        //
        //    { "mul", 30 },
        //    { "fmul", 30 },
        //    { "sdiv", 30 },
        //    { "udiv", 30 },
        //    { "fdiv", 30 },
        //    { "srem", 30 },
        //    { "urem", 30 },
        //
        //    { "add", 40 },
        //    { "fadd", 40 },
        //    { "sub", 40 },
        //    { "fsub", 40 },
        //
        //    { "shl", 50 },
        //    { "lshr", 50 },
        //    { "ashr", 50 },
        //
        //    { "icmp", 60 },
        //    { "fcmp", 60 },
        //
        //    { "and", 80 },
        //    { "xor", 90 },
        //    { "or", 100 },
        //};

        //    private int I;
        //
        //    #region Helper
        //
        //    private List<Lexem> Lexems;
        //
        //    private Lexem Current => Lexems[I];
        //
        //    private Lexem Next => Lexems[I + 1];
        //
        //    private Lexem Prev => Lexems[I - 1];
        //
        //    [DebuggerStepThrough]
        //    private void Forward(int count = 1) => I += count;
        //
        //    [DebuggerStepThrough]
        //    private void Backward(int count = 1) => I -= count;
        //
        //    private CordyType Type { get; }
        //
        //    public static List<CordyType> UnknownTypes { get; } = new List<CordyType>();
        //
        //    private string FileName => Type.Name;
        //
        //    #endregion
        //
        //    private CodegenListener Listener { get; }
        //
        //    internal CordyParser(CordyType type, CodegenListener listener)
        //    {
        //        Listener = listener;
        //        Type = type;
        //    }
        //
        //    // numberexpr ::= number
        //    private BasicNode ParseIntExpr()
        //    {
        //        Forward();
        //        return new IntegerNode(Prev.Value);
        //    }
        //
        //    // parenexpr ::= '(' expression ')'
        //    private BasicNode ParserParenExpr()
        //    {
        //        Forward();
        //        var v = ParseExpression();
        //        if (v == null)
        //            return null;
        //
        //        if (Current.Value != ")")
        //        {
        //            Compiler.Error("Expected ')'", FileName, Current.Pos, "Parser");
        //            return null;
        //        }
        //        Forward();
        //        return v;
        //    }
        //
        //    // identfierexpr
        //    //   ::= identifier
        //    //   ::= identifier '(' expression* ')'
        //    private BasicNode ParseIdentifierExpr()
        //    {
        //        var name = Current.Value;
        //        Forward();
        //
        //        if (Current.Value != "(")
        //            return new VarNode(name);
        //
        //        Forward();
        //        var args = new List<ExprAST>();
        //        if (Current.Value != ")")
        //        {
        //            while (true)
        //            {
        //                var arg = ParseExpression();
        //                if (arg != null)
        //                    args.Add(arg);
        //                else
        //                    return null;
        //
        //                if (Current.Value == ")")
        //                    break;
        //
        //                if (Current.Value != ",")
        //                {
        //                    Compiler.Error("Expected ')' or ',' in arg list", FileName, Current.Pos, "Parser");
        //                    return null;
        //                }
        //                Forward();
        //            }
        //        }
        //
        //        Forward();
        //
        //        return new CallExprAST(name, args);
        //    }
        //
        //    private ExprAST ParsePrimary()
        //    {
        //        switch (Current.Type)
        //        {
        //            default:
        //                Compiler.Error($"Unexpected token {Current.Value}", FileName, Current.Pos, "Parser");
        //                return null;
        //            case eLexemType.Bracket when Current.Value == "(":
        //                return ParserParenExpr();
        //
        //            case eLexemType.Identifier:
        //                return ParseIdentifierExpr();
        //
        //            case eLexemType.IntegerBinary:
        //            case eLexemType.IntegerOctal:
        //            case eLexemType.IntegerDecimal:
        //            case eLexemType.IntegerHexadecimal:
        //                return ParseIntExpr();
        //        }
        //    }
        //
        //    //int GetOperPrecedence()
        //    //{
        //    //
        //    //}
        //
        //    private ExprAST ParseExpression() => throw new NotImplementedException();
        //}
    }
}
