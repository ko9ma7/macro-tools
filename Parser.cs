namespace Macroc
{
    internal sealed class Parser
    {
        public enum Opcode
        {
            Add = (byte)0x00,
        }

        private enum VarType
        {
            Int,
            Float
        }

        private List<Token> Toks;
        private Token Current;
        private int CurPos;
        public Parser(List<Token> toks)
        {
            Toks = toks;
            Current = Toks[0];
            CurPos = 0;
        }

        private TokenType PeekType()
        {
            return Toks[CurPos + 1].Type;
        }
        
        private void Next(bool allowEOF)
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
                        return bytes;
                    case TokenType.Ident:
                        if (PeekType() == TokenType.Operator)
                        {
                            // Expression
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