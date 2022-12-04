namespace Macroc
{
    internal sealed class Parser
    {
        public enum Opcode
        {
            Ldvar = 0x00,
            Ldfloat,
            Ldint,
            Ldstring,
            Stovar,
            Add = 0x10,
            Sub,
            Mult,
            Div,
        }

        public enum Register
        {
            R1,
            R2,
            R3,
            R4
        }

        private enum VarType
        {
            Int,
            Float
        }

        private List<Token> Toks;
        private Token Current;
        private int CurPos;
        private bool IsValid;
        private Dictionary<string, VarType> VarTable;
        private Dictionary<string, int> FuncTable;
        public Parser(List<Token> toks)
        {
            Toks = toks;
            Current = Toks[0];
            CurPos = 0;
            IsValid = true;
            VarTable = new();
            FuncTable = new();
        }

        private TokenType PeekType()
        {
            return Toks[CurPos + 1].Type;
        }
        
        private void Next(bool allowEOF = false)
        {
            CurPos++;
            if (CurPos >= Toks.Count)
            {
                if (!allowEOF)
                {
                    Console.WriteLine("Unexpected end of file");
                    Environment.Exit(-2);
                }
                Current = new EOSToken(0);
                return;
            }
            Current = Toks[CurPos];
        }

        private List<byte> ParseOperandLoad(Token operand, Register reg)
        {
            List<byte> bytes = new();

            switch (operand.Type)
            {
                case TokenType.Ident:
                string Ident = ((IdentToken)operand).Ident;
                break;
                case TokenType.Int:
                break;
                case TokenType.Float:
                break;
                default:
                Console.WriteLine($"Error: expected number or ident (Line {operand.Line + 1}");
                IsValid = false;
                break;
            }

            return bytes;
        }

        public List<byte> Parse()
        {
            List<byte> bytes = new();
            Dictionary<string, int> funcTable = new();
            Dictionary<string, VarType> varTable = new();

            while (true)
            {
                switch (Current.Type)
                {
                    case TokenType.EOS:
                        if (!IsValid) Environment.Exit(-4);
                        return bytes;
                    case TokenType.Ident:
                        if (PeekType() == TokenType.Operator)
                        {
                            string assignee = ((IdentToken)Current).Ident;
                            Next();

                            if (((OperatorToken)Current).Operator != OperatorType.Assign)
                            {
                                Console.WriteLine($"Error: Expected assignment operator (Line {Current.Line + 1}) ");
                                IsValid = false;
                            }

                            Next();
                            Token left = Current;
                            Next();
                            Token op = Current;
                            Next();
                            Token right = Current;


                        }
                        else
                        {
                            // Function
                        }
                        break;
                }

            }
        }
    }

    static class OpcodeEnumExt
    {
        public static byte Value(this Parser.Opcode code ) { return (byte)code; }
    }
}