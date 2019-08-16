using Cordy;
using Cordy.AST;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests
{
    public class ParserTests
    {
        //[InlineData("Vector:3{Int:32:false} Name")]
        [Theory]
        [InlineData("Type Name")]
        [InlineData("Vector Name")]
        [InlineData("Vector{Int} Name")]
        [InlineData("Vector:3{Int:32:false} Name")]
        [InlineData("Dict{int,int} Name")]
        //[InlineData("Dict{int,int}:10 Name")]
        [InlineData("Dict{int,List{int}} Name")]
        public void TestTypeParsing(string input)
        {
            var lex = new Lexer();
            lex.Lexems = lex.Tokenize(input);
            lex.I = 1;
            lex.Lexems.Add(new Lexem(eLexemType.EOF, (0, 0)));
            var par = new Parser(lex);
            var actual = par.ParseType();
            Assert.Equal(input.Split(' ')[0], actual.ToString());
        }
    }
}
