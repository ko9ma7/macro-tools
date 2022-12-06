namespace Macroc
{
    internal sealed class Parser
    {
        public enum Opcode
        {
            LDVar = (byte)0x00,
            LDImm,
            Ldstring,
            StoreVar,
            FloatOperation,
            IntOperation,
            Call,
            StartFunc,
            EndFunc,
            Add = (byte)0x10,
            Sub,
            Mult,
            Div,
            SetupFrame = (byte)0x20,
            FreeFrame,
        }

        public enum Register
        {
            R1 = (byte)0x00,
            R2,
        }

        private enum VarType
        {
            Int,
            Float
        }

        private struct VarEntry
        {
            public VarType Type;
            public int Offset;
            public VarEntry(VarType type, int offset)
            {
                Type = type;
                Offset = offset;
            }
        }

        private List<Token> Toks;
        private Token Current;
        private int CurPos;
        private bool IsValid;
        private int StackStackOffset;
        private int GlobalStackOffset;
        private int StackOffset
        {
            get => InFunction ? StackStackOffset : GlobalStackOffset;
            set 
            {
                if (InFunction) StackStackOffset = value;
                else GlobalStackOffset = value;
            }
        }
        private bool InFunction;
        private Dictionary<string, VarEntry> StackVarTable;
        private Dictionary<string, VarEntry> GlobalVarTable;
        private Dictionary<string, VarEntry> VarTable { get => InFunction ? StackVarTable : GlobalVarTable; }
        private Dictionary<string, int> FuncTable;
        public Parser(List<Token> toks)
        {
            Toks = toks;
            Current = Toks[0];
            CurPos = 0;
            IsValid = true;
            StackVarTable = new();
            GlobalVarTable = new();
            FuncTable = new();
            StackStackOffset = 0;
            GlobalStackOffset = 0;
            InFunction = false;
        }

        private void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
            IsValid = false;
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
                    Error("Unexpected end of file");
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
                if (!AssertVarExists((IdentToken)tok))
                    return VarType.Int;

                return VarTable[((IdentToken)tok).Ident].Type;

                default:
                Error($"Error: Cannot get type of typeless token (Line {tok.Line + 1})");
                return VarType.Int;
            }
        }

        private bool AssertVarExists(IdentToken tok)
        {
            if (!VarTable.ContainsKey(((IdentToken)tok).Ident))
            {
                Error($"Error: Undeclared name {((IdentToken)tok).Ident} (Line {tok.Line + 1})");
                return false;
            }

            return true;
        }

        // <summary>
        // Convert operator type to its matching opcode
        // </summary>
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

                case OperatorType.Assign:
                Error($"Error: Generic assign has no opcode");
                return 0xFF;

                default:
                return 0xFF;
            }
        }

        // <summary>
        //  Checks if a token is a float, int, or ident
        // </summary>
        private bool IsNumType(Token token)
        {
            return token.Type == TokenType.Int || token.Type == TokenType.Int || token.Type == TokenType.Ident;
        }

        // <summary>
        // Parse a load operation for an arithmetic operation
        // </summary>
        private List<byte> ParseOperandLoad(Token operand, Register reg)
        {
            List<byte> bytes = new();

            switch (operand.Type)
            {
                case TokenType.Ident:
                // Ensure variable exists
                IdentToken token = (IdentToken)operand;
                if (!AssertVarExists(token))
                    break;

                // Write opcode, register, and offset
                bytes.Add(Opcode.LDVar.Value());
                bytes.Add(reg.Value());
                bytes.AddRange(BitConverter.GetBytes(VarTable[token.Ident].Offset));
                break;

                case TokenType.Float:
                case TokenType.Int:
                // Write opcode and value
                bytes.Add(Opcode.LDImm.Value());
                bytes.Add(reg.Value());
                bytes.AddRange(BitConverter.GetBytes(((IntToken)operand).Value));
                break;

                default:
                Error($"Error: expected number or ident (Line {operand.Line + 1}");
                break;
            }

            return bytes;
        }

        // <summary>
        // Parse an expression beginning with an ident
        // <summary>
        private List<byte> ParseIdent()
        {
            List<byte> bytes = new();
            if (PeekType() == TokenType.Operator)
            {
                // Must be an assignment
                // Store the asignee
                string assignee = ((IdentToken)Current).Ident;
                Next();

                // Make sure we are assigning
                if (((OperatorToken)Current).Operator != OperatorType.Assign)
                {
                    Error($"Error: Expected assignment operator (Line {Current.Line + 1}) ");
                    
                    return bytes;
                }

                // Get expression tokens
                Next();
                Token left = Current;
                Next();
                Token op = Current;
                Next();
                Token right = Current;

                // Validate token types
                if (!IsNumType(left))
                {
                    Error($"Error: Expected number type (Line {left.Line + 1})");
                    return bytes;
                }

                if (!IsNumType(right))
                {
                    Error($"Error: Expected number type (Line {left.Line + 1})");
                    return bytes;
                }

                if (op.Type != TokenType.Operator)
                {
                    Error($"Error: Expected operator (Line {left.Line + 1})");
                    return bytes;
                }

                // Make sure left and right types are the same
                VarType leftType = DataTypeFromTok(left);
                VarType rightType = DataTypeFromTok(right);

                if (leftType != rightType || leftType != VarTable[assignee].Type)
                {
                    Error($"Error: Operand types do not match (Line {op.Line + 1})");
                    return bytes;
                }
                // Write operation type
                bytes.Add(((leftType == VarType.Int ? Opcode.IntOperation : Opcode.FloatOperation)).Value());
                
                // Write load operations
                bytes.AddRange(ParseOperandLoad(left, Register.R1));
                bytes.AddRange(ParseOperandLoad(right, Register.R2));

                // Write operator
                bytes.Add(GetOperatorOpcode(((OperatorToken)op).Operator));

                // Write store operation
                bytes.Add(Opcode.StoreVar.Value());
                bytes.AddRange(BitConverter.GetBytes(VarTable[assignee].Offset));
            }
            else
            {
                // Function
            }

            return bytes;
        }

        // <summary>
        // Parse an expression beginning with a builtin
        // </summary>
        private List<byte> ParseBuiltin()
        {
            List<byte> bytes = new();
            switch (((BuiltinToken)Current).Builtin)
            {
                case Builtin.Start:
                {
                    // Do not allow function definition inside of a function
                    if (InFunction)
                    {
                        Error($"Error: Cannot define function in function (Line {Current.Line + 1})");
                        break;
                    }
                    // Validate token types
                    if (PeekType() != TokenType.Ident)
                    {
                        Error($"Error: Expected ident (Line {Current.Line + 1}");
                        break;
                    }
                    Next();
                    InFunction = true;
                    IdentToken identTok = (IdentToken)Current;

                    bytes.Add(Opcode.StartFunc.Value());
                    FuncTable.Add(identTok.Ident, bytes.Count());

                    // Setup stack frame
                    StackOffset = 0;
                    VarTable.Clear();
                    break;
                }

                case Builtin.End:
                    // Only allow end at end of function
                    if (!InFunction)
                    {
                        Error($"Error: 'End' only valid at end of function (Line {Current.Line + 1})");
                        break;
                    }
                    InFunction = false;

                    bytes.Add(Opcode.EndFunc.Value());
                    break;

                case Builtin.Int:
                {
                    // Must be a variable definition
                    // Validate token types
                    if (PeekType() != TokenType.Ident)
                    {
                        Error($"Error: Expected ident (Line {Current.Line + 1}");
                        break;
                    }
                    Next();
                    IdentToken identTok = (IdentToken)Current;
                    // Setup variable entry
                    StackOffset += 4;
                    VarEntry entry = new(VarType.Int, StackOffset);
                    VarTable.Add(identTok.Ident, entry);
                    break;
                }

                case Builtin.Float:
                {
                    // Must be a variable definition
                    // Validate token types
                    if (PeekType() != TokenType.Ident)
                    {
                        Error($"Error: Expected ident (Line {Current.Line + 1}");
                        break;
                    }
                    Next();
                    IdentToken identTok = (IdentToken)Current;
                    // Setup variable entry
                    StackOffset += 4;
                    VarEntry entry = new(VarType.Float, StackOffset);
                    VarTable.Add(identTok.Ident, entry);
                    break;
                }

                default:
                break;
            }

            return bytes;
        }

        // <summary>
        // Parse the data currently bound to the parser
        // </summary>
        public List<byte> Parse()
        {
            List<byte> bytes = new();

            while (true)
            {
                switch (Current.Type)
                {
                    case TokenType.EOS:
                        if (!IsValid) Environment.Exit(-4);
                        return bytes;

                    case TokenType.Ident:
                        bytes.AddRange(ParseIdent());
                        break;

                    case TokenType.Builtin:
                        bytes.AddRange(ParseBuiltin());
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