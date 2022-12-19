namespace MacroCompiler
{

    public class Token
    {
        public enum TokenType
        {
            Operator,
            String,
            Builtin,
            Ident,
            Int,
            Float,
            EOS,
            ENDL,
        }

        public enum OperatorType
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Assign,
            LeftParen,
            RightParen,
        }

        public enum BuiltinType
        {
            Move = 0x00,
            Drag,
            Click,
            Type,
            Mod,
            Delay,
            Display,
            Start = 0x20,
            End,
            Int = 0x40,
            Float,
        }

        public TokenType Type { get; }
        public int Line { get; }
        protected Token(TokenType type, int line)
        {
            Type = type;
            Line = line;
        }
    }

    internal sealed class OperatorToken : Token
    {
        public OperatorType Operator { get; }
        public OperatorToken(OperatorType op, int line) : base(TokenType.Operator, line)
        {
            Operator = op;
        }
    }

    internal sealed class IdentToken : Token
    {
        public string Ident { get; }
        public IdentToken(string ident, int line) : base(TokenType.Ident, line)
        {
            Ident = ident;
        }
    }

    internal sealed class StringToken : Token
    {
        public string String { get; }
        public StringToken(string val, int line) : base(TokenType.String, line)
        {
            String = val;
        }
    }

    internal sealed class BuiltinToken : Token
    {
        public BuiltinType Builtin { get; }
        public BuiltinToken(BuiltinType builtin, int line) : base(TokenType.Builtin, line)
        {
            Builtin = builtin;
        }
    }

    internal sealed class IntLiteral : Token
    {
        public int Value { get; }
        public IntLiteral(int value, int line) : base(TokenType.Int, line)
        {
            Value = value;
        }
    }

    internal sealed class FloatLiteral : Token
    {
        public float Value { get; }
        public FloatLiteral(float value, int line) : base(TokenType.Float, line)
        {
            Value = value;
        }
    }

    internal sealed class EOSToken : Token
    {
        public EOSToken(int line) : base(TokenType.EOS, line) {}
    }

    internal sealed class ENDLToken :Token
    {
        public ENDLToken(int line) : base(TokenType.ENDL, line) {}
    }
}