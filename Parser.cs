namespace Macroc
{
    internal sealed class Parser
    {
        public enum Opcode
        {
            Ldfloatvar = (byte)0x00,
            Ldintvar,
            Ldfloat,
            Ldint,
            Ldstring,
            Stovar,
            Floatop,
            Intop,
            Declint,
            Declfloat,
            Call,
            Add = (byte)0x10,
            Sub,
            Mult,
            Div,
        }

        public enum Register
        {
            I1 = (byte)0x00,
            I2,
            F1,
            F2
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

        private List<byte> IdentToBytes(string ident)
        {
            List<byte> bytes = new();
            bytes.AddRange(BitConverter.GetBytes(ident.Length));
            bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(ident));
            return bytes;
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

        private VarType DataTypeFromTok(Token tok)
        {
            switch (tok.Type)
            {
                case TokenType.Int:
                return VarType.Int;
                case TokenType.Float:
                return VarType.Float;
                case TokenType.Ident:
                if (!VarTable.ContainsKey(((IdentToken)tok).Ident))
                {
                    Console.WriteLine($"Error: Undeclared name {((IdentToken)tok).Ident} (Line {tok.Line + 1})");
                    IsValid = false;
                    return VarType.Int;
                }
                return VarTable[((IdentToken)tok).Ident];
                default:
                return VarType.Int;
            }
        }

        private byte GetOperatorOpcode(OperatorType op)
        {
            switch (op)
            {
                case OperatorType.Add:
                return Opcode.Add.Value();
                case OperatorType.Subtract:
                return Opcode.Sub.Value();
                case OperatorType.Multiply:
                return Opcode.Mult.Value();
                case OperatorType.Divide:
                return Opcode.Div.Value();
                default:
                return 0xFF;
            }
        }

        private List<byte> ParseOperandLoad(Token operand, Register reg)
        {
            List<byte> bytes = new();

            switch (operand.Type)
            {
                case TokenType.Ident:

                IdentToken token = (IdentToken)operand;
                if (!VarTable.ContainsKey(token.Ident))
                {
                    Console.WriteLine($"Error: name {token.Ident} is undefined (Line {token.Line + 1}");
                    IsValid = false;
                    break;
                }

                switch (VarTable[token.Ident])
                {
                    case VarType.Int:
                    bytes.Add(Opcode.Ldintvar.Value());
                    break;
                    case VarType.Float:
                    bytes.Add(Opcode.Ldfloatvar.Value());
                    break;
                }
                bytes.AddRange(IdentToBytes(token.Ident));
                bytes.Add(reg.Value());
                break;
                case TokenType.Int:
                bytes.Add(Opcode.Ldint.Value());
                bytes.AddRange(BitConverter.GetBytes(((IntToken)operand).Value));
                break;
                case TokenType.Float:
                bytes.Add(Opcode.Ldfloatvar.Value());
                bytes.AddRange(BitConverter.GetBytes(((FloatToken)operand).Value));
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

                            VarType leftType = DataTypeFromTok(left);
                            VarType rightType = DataTypeFromTok(right);

                            if (leftType != rightType)
                            {
                                Console.WriteLine($"Error: Operand types do not match (Line {op.Line + 1})");
                                IsValid = false;
                                break;
                            }

                            bytes.AddRange(ParseOperandLoad(left, leftType == VarType.Int ? Register.I1 : Register.F1));
                            bytes.AddRange(ParseOperandLoad(right, rightType == VarType.Int ? Register.I2 : Register.F2));
                            bytes.Add(((leftType == VarType.Int ? Opcode.Intop : Opcode.Floatop)).Value());
                            bytes.Add(GetOperatorOpcode(((OperatorToken)op).Operator));
                        }
                        else
                        {
                            // Function
                        }
                        break;
                    case TokenType.Builtin:
                        switch (((BuiltinToken)Current).Builtin)
                        {
                            case Builtin.Int:
                                if (PeekType() != TokenType.Ident)
                                {
                                    Console.WriteLine($"Error: Expected ident (Line {Current.Line + 1}");
                                    IsValid = false;
                                    break;
                                }
                                Next();
                                bytes.Add(Opcode.Declint.Value());
                                IdentToken identTok = (IdentToken)Current;
                                bytes.AddRange(IdentToBytes((identTok.Ident)));
                                break;
                            case Builtin.Float:
                                break;
                            default:
                            break;
                        }
                        break;
                    default:
                        break;
                }

                Next();
            }
        }
    }

    static class EnumByteExt
    {
        public static byte Value(this Parser.Opcode code ) { return (byte)code; }
        public static byte Value(this Parser.Register reg ) { return (byte)reg; }
    }
}