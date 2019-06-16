using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MultiRegexSearcher
{
    public enum TokenType
    {
        Operator,
        Operand
    }

    public class Token : IComparable<Token>
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }

        private static Dictionary<string, int> operatorPrecedence = new Dictionary<string, int>();
        static Token()
        {
            operatorPrecedence = new Dictionary<string,int>()
                { 
                    {"(", 1}, 
                    {")", 1},
                    {"*", 2}, 
                    {"+", 2},
                    {"?", 2}, 
                    {"concat", 3},
                    {"|", 4}
                };
        }

        public Token(TokenType type, string Value)
        {
            this.Type = type;
            this.Value = Value;
        }

        public static Token GetTokenAt(string str, ref int index)
        {
            if (index > str.Length - 1)
                throw new ArgumentOutOfRangeException();

            Token foundToken = null;
            char[] operators = operatorPrecedence.Keys.Where(key => key.Length == 1).Select(key => key[0]).ToArray();
            if (operators.Contains(str[index]))
                foundToken = new Token(TokenType.Operator, str[index].ToString());
            else if (str[index] == '[')
            {
                Match match = Regex.Match(str, "(?<!\\)]");
                if (!match.Success)
                    throw new Exception("RegEx {0} is malformed. There was no ending \"]\" specified");

                foundToken = new Token(TokenType.Operand, str.Substring(index, match.Index - index));
                index += match.Index - index;
            }
            return foundToken ?? new Token(TokenType.Operand, str[index].ToString());
        }

        public int CompareTo(Token other)
        {
            if (this.Type != other.Type)
                throw new Exception("These two should never be compared");

            return operatorPrecedence[this.Value].CompareTo(operatorPrecedence[other.Value]);
        }

        public static bool operator <(Token first, Token second)
        {
            return first.CompareTo(second) > 0;
        }

        public static bool operator >(Token first, Token second)
        {
            return first.CompareTo(second) < 0;
        }

        public override string ToString()
        {
            return this.Value;
        }
    }
}
