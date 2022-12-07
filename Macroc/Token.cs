namespace Macroc
{
    enum TokenType
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

    enum OperatorType
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Assign,
        LeftParen,
        RightParen,
    }

    enum Builtin
    {
        Move = 0x00,
        Drag,
        Click,
        Type,
        Mod,
        Start = 0x20,
        End,
        Int = 0x40,
        Float,
    }

    internal class Token
    {
        public TokenType Type;
        public int Line;
        protected Token(TokenType type, int line)
        {
            Type = type;
            Line = line;
        }
    }

    internal sealed class OperatorToken : Token
    {
        public OperatorType Operator;
        public OperatorToken(OperatorType op, int line) : base(TokenType.Operator, line)
        {
            Operator = op;
        }
    }

    internal sealed class IdentToken : Token
    {
        public string Ident;
        public IdentToken(string ident, int line) : base(TokenType.Ident, line)
        {
            Ident = ident;
        }
    }

    internal sealed class StringToken : Token
    {
        public string String;
        public StringToken(string val, int line) : base(TokenType.String, line)
        {
            String = val;
        }
    }

    internal sealed class BuiltinToken : Token
    {
        public Builtin Builtin;
        public BuiltinToken(Builtin builtin, int line) : base(TokenType.Builtin, line)
        {
            Builtin = builtin;
        }
    }

    internal sealed class IntToken : Token
    {
        public int Value;
        public IntToken(int value, int line) : base(TokenType.Int, line)
        {
            Value = value;
        }
    }

    internal sealed class FloatToken : Token
    {
        public float Value;
        public FloatToken(float value, int line) : base(TokenType.Float, line)
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