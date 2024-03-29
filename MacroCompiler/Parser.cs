using MacroCommon;

namespace MacroCompiler
{
    internal sealed class Parser
    {
        public enum Opcode
        {
            LDVar = 0x00,
            LDImm,
            PushVar,
            PushImm,
            StoreVar,
            FloatOperation,
            IntOperation,
            Call,
            StartFunc,
            EndFunc,
            Halt,
            Display,
            Move,
            Delay,
            Add = 0x20,
            Sub,
            Mult,
            Div,
            SetupFrame = 0x40,
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

        public const int Version = 1;
        private List<Token> Toks;
        private List<byte> bytes;
        private Token Current;
        private int CurPos;
        private bool IsValid;
        private int StackStackOffset;
        private int GlobalStackOffset;
        private bool IsReady;
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
        private bool InFunctionCall;
        private readonly Dictionary<string, VarEntry> StackVarTable;
        private readonly Dictionary<string, VarEntry> GlobalVarTable;
        private Dictionary<string, VarEntry> VarTable { get => InFunction ? StackVarTable : GlobalVarTable; }
        private readonly Dictionary<string, int> FuncTable;
        public Parser()
        {
            CurPos = 0;
            IsValid = true;
            StackVarTable = new();
            GlobalVarTable = new();
            FuncTable = new();
            StackStackOffset = 0;
            GlobalStackOffset = 0;
            InFunction = false;
            InFunctionCall = false;
            IsReady = false;
        }

        public void BindData(List<Token> toks)
        {
            Toks = toks;
            bytes = new((int)(Toks.Count * 2.5f)); // Rough ratio of tokens to bytes (needs more data)
            Current = Toks[0];
            IsReady = true;
        }

        private void Error(string message)
        {
            Logger.Error(message);
            IsValid = false;
        }

        private Token.TokenType PeekType()
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
                    throw new EndOfStreamException();
                }
                Current = new EOSToken(0);
                return;
            }
            Current = Toks[CurPos];
        }

        private void Back()
        {
            CurPos--;
            Current = Toks[CurPos];
        }

        private bool AssertVarExists(IdentToken tok)
        {
            if (!VarTable.ContainsKey((tok).Ident))
            {
                Error($"Undeclared name {(tok).Ident} (Line {tok.Line + 1})");
                return false;
            }

            return true;
        }

        private bool AssertVarExists(string ident)
        {
            if (!VarTable.ContainsKey(ident))
            {
                Error($"Undeclared name '{ident}' (Line {Current.Line + 1})");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Convert operator type to its matching opcode
        /// </summary>
        private byte GetOperatorOpcode(Token.OperatorType op)
        {
            switch (op)
            {
                case Token.OperatorType.Add:
                return Opcode.Add.Value();

                case Token.OperatorType.Subtract:
                return Opcode.Sub.Value();

                case Token.OperatorType.Multiply:
                return Opcode.Mult.Value();

                case Token.OperatorType.Divide:
                return Opcode.Div.Value();

                case Token.OperatorType.Assign:
                Error($"Generic assign is not valid (Line {Current.Line + 1})");
                return 0xFF;

                default:
                return 0xFF;
            }
        }

        private static int GetOperatorPrecedence(Token.OperatorType op) =>
            op switch
            {
                Token.OperatorType.Multiply or Token.OperatorType.Divide => 3,
                Token.OperatorType.Add or Token.OperatorType.Subtract => 2,
                _ => 0
            };

        private void ParseExprIntLit(VarType type, ref bool expectOperator)
        {
            if (expectOperator)
            {
                Error($"Expected operator (Line {Current.Line + 1})");
                return;
            }

            bytes.Add(Opcode.PushImm.Value());
            int value = ((IntLiteral)Current).Value;

            // if expression is float-typed, push float. otherwise, push int
            if (type == VarType.Int) bytes.AddRange(BitConverter.GetBytes(value));
            else bytes.AddRange(BitConverter.GetBytes((float)value));

            expectOperator = true;
        }
        private void ParseExprFloatLit(VarType type, ref bool expectOperator)
        {
            if (expectOperator)
            {
                Error($"Expected operator (Line {Current.Line + 1})");
                return;
            }

            bytes.Add(Opcode.PushImm.Value());
            float value = ((FloatLiteral)Current).Value;

            // if expression is float-typed, push float. otherwise, push int
            if (type == VarType.Float) bytes.AddRange(BitConverter.GetBytes(value));
            else bytes.AddRange(BitConverter.GetBytes((int)value));

            expectOperator = true;
        }

        private void ParseExprIdent(VarType type, ref bool expectOperator)
        {
            if (expectOperator)
            {
                Error($"Expected operator (Line {Current.Line + 1})");
                return;
            }


            IdentToken identToken = (IdentToken)Current;
            if (!AssertVarExists(identToken)) return;

            if (VarTable[identToken.Ident].Type != type)
            {
                Logger.Error($"Var '{identToken.Ident}' has invalid type for expression");
                IsValid = false;
                expectOperator = true;
                return;
            }

            bytes.Add(Opcode.PushVar.Value());
            bytes.AddRange(BitConverter.GetBytes(VarTable[identToken.Ident].Offset));

            expectOperator = true;
        }

        private void ParseExprOperator(Stack<Token.OperatorType> operatorStack, ref bool expectOperator)
        {
            Token.OperatorType operation = ((OperatorToken)Current).Operator;
            // if operator stack is empty, we must push
            if (operatorStack.Count == 0)
            {
                operatorStack.Push(operation);
                expectOperator = false;
                return;
            }

            int currentPrec = GetOperatorPrecedence(operation);
            int prevPrec = GetOperatorPrecedence(operatorStack.Peek());

            if (operation == Token.OperatorType.LeftParen)
            {
                // Left paren must take place of value
                if (expectOperator)
                {
                    Error($"Expected value, got left parenthesis (Line {Current.Line + 1})");
                    return;
                }

                operatorStack.Push(operation);
                return;
            }

            if (operation == Token.OperatorType.RightParen)
            {
                // Right paren must take place of operator
                if (!expectOperator)
                {
                    Error($"Expected operator, got right parenthesis (Line {Current.Line + 1})");
                    return;
                }
                
                // loop until we find a left parenthesis
                while (operatorStack.Peek() != Token.OperatorType.LeftParen)
                {
                    Token.OperatorType op = operatorStack.Pop();
                    bytes.Add(GetOperatorOpcode(op));

                    // if stack is empty, then we haven't found a matching parenthesis
                    // this indicates unbalanced parentheses
                    if (operatorStack.Count == 0)
                    {
                        Error($"Unbalanced parentheses (Line {Current.Line + 1})");
                        return;
                    }
                }
                // pop leftover left parenthesis
                operatorStack.Pop();
                return;
            }

            expectOperator = false;

            // is precedences are less-than or equal, pop previous and push current
            if (currentPrec <= prevPrec)
            {
                Token.OperatorType op = operatorStack.Pop();
                bytes.Add(GetOperatorOpcode(op));
                operatorStack.Push(operation);
                return;
            }
            // if current has greater precedence
            else
            {
                operatorStack.Push(operation);
                return;
            }
        }


        private void ParseExpression(VarType type)
        {
            Stack<Token.OperatorType> operatorStack = new();
            bool expectOperator = false;

            Next();

            while (Current.Type != Token.TokenType.ENDL && Current.Type != Token.TokenType.EOS)
            {
                if (expectOperator && Current.Type != Token.TokenType.Operator)
                {
                    if (InFunctionCall)
                    {
                        Back();
                        break;
                    }
                    Error($"Expected operator (Line {Current.Line + 1})");
                    return;
                }

                switch (Current.Type)
                {
                    case Token.TokenType.Int:
                        ParseExprIntLit(type, ref expectOperator);
                        break;

                    case Token.TokenType.Float:
                        ParseExprFloatLit(type, ref expectOperator);
                        break;

                    case Token.TokenType.Ident:
                        ParseExprIdent(type, ref expectOperator);
                        break;

                    case Token.TokenType.Operator:
                        ParseExprOperator(operatorStack, ref expectOperator);
                        break;

                    default:
                        Error($"Invalid term in expression (Line {Current.Line + 1})");
                        return;
                }

                Next();
            }

            if (!expectOperator)
            {
                Error($"Expected value after operator (Line {Current.Line + 1})");
            }
            while (operatorStack.TryPop(out var op))
            {
                bytes.Add(GetOperatorOpcode(op));
            }

            return;
        }

        /// <summary>
        /// Parse an expression beginning with an ident
        /// <summary>
        private void ParseIdent()
        {
            if (PeekType() == Token.TokenType.Operator)
            {
                // Must be an assignment
                // Store the asignee
                string assignee = ((IdentToken)Current).Ident;
                if (!AssertVarExists(assignee)) return;
                Next();

                // Make sure we are assigning
                if (((OperatorToken)Current).Operator != Token.OperatorType.Assign)
                {
                    Error($"Expected assignment operator (Line {Current.Line + 1}) ");
                    return;
                }

                VarType resultType = VarTable[assignee].Type;

                // Write operation type
                bytes.Add(((resultType == VarType.Int ? Opcode.IntOperation : Opcode.FloatOperation)).Value());

                ParseExpression(resultType);

                // Write store operation
                bytes.Add(Opcode.StoreVar.Value());
                bytes.AddRange(BitConverter.GetBytes(VarTable[assignee].Offset));
            }
            else
            {
                // Function
            }

            return;
        }

        /// <summary>
        /// Parse a builtin statement
        /// </summary>

        private void ParseBuiltin()
        {
            switch (((BuiltinToken)Current).Builtin)
            {
                case Token.BuiltinType.Start:
                {
                    // Do not allow function definition inside of a function
                    if (InFunction)
                    {
                        Error($"Cannot define function in function (Line {Current.Line + 1})");
                        break;
                    }

                    // Validate token types
                    if (PeekType() != Token.TokenType.Ident)
                    {
                        Error($"Expected ident (Line {Current.Line + 1}");
                        break;
                    }

                    Next();
                    InFunction = true;
                    IdentToken identTok = (IdentToken)Current;

                    bytes.Add(Opcode.StartFunc.Value());
                    FuncTable.Add(identTok.Ident, bytes.Count);

                    // Setup stack frame
                    StackOffset = 0;
                    VarTable.Clear();

                    break;
                }

                case Token.BuiltinType.End:
                    // Only allow end at end of function
                    if (!InFunction)
                    {
                        Error($"'End' only valid at end of function (Line {Current.Line + 1})");
                        break;
                    }
                    InFunction = false;

                    bytes.Add(Opcode.EndFunc.Value());
                    break;

                case Token.BuiltinType.Int:
                {
                    // Must be a variable definition
                    // Validate token types
                    if (PeekType() != Token.TokenType.Ident)
                    {
                        Error($"Expected ident (Line {Current.Line + 1}");
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

                case Token.BuiltinType.Float:
                {
                    // Must be a variable definition
                    // Validate token types
                    if (PeekType() != Token.TokenType.Ident)
                    {
                        Error($"Expected ident (Line {Current.Line + 1}");
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

                case Token.BuiltinType.Display:
                {
                    // Must be a variable next
                    // Validate token types
                    if (PeekType() != Token.TokenType.Ident)
                    {
                        Error($"Expected ident (Line {Current.Line + 1}");
                        break;
                    }
                    Next();
                    string ident = ((IdentToken)Current).Ident;

                    if (!AssertVarExists(ident)) return;

                    bytes.Add(VarTable[ident].Type == VarType.Int ? Opcode.IntOperation.Value() : Opcode.FloatOperation.Value());
                    bytes.Add(Opcode.Display.Value());
                    bytes.AddRange(BitConverter.GetBytes(VarTable[ident].Offset));

                    break;
                }

                case Token.BuiltinType.Move:
                {
                    InFunctionCall = true;
                    bytes.Add(Opcode.IntOperation.Value());
                    ParseExpression(VarType.Int);
                    ParseExpression(VarType.Int);
                    bytes.Add(Opcode.Move.Value());
                    InFunctionCall = false;

                    break;
                }

                case Token.BuiltinType.Delay:
                {
                    InFunctionCall = true;
                    bytes.Add(Opcode.FloatOperation.Value());
                    ParseExpression(VarType.Float);
                    bytes.Add(Opcode.Delay.Value());
                    InFunctionCall = false;

                    break;
                }

                default:
                break;
            }

            return;
        }

        private void WriteGlobalStackOffset()
        {
            byte[] offset = BitConverter.GetBytes(GlobalStackOffset);
            for (int i = 0; i < 4; i++)
            {
                bytes[i + 5] = offset[i];
            }
        }

        /// <summary>
        /// Parse the data currently bound to the parser
        ///</summary>
        public List<byte> Parse()
        {
            if (!IsReady)
            {
                Logger.Error("No data bound to parser...");
                throw new ParserException("No dat abound to parser.");
            }
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(new List<byte>() {Opcode.SetupFrame.Value(), 0, 0, 0, 0});

            while (true)
            {
                switch (Current.Type)
                {
                    case Token.TokenType.EOS:
                        if (!IsValid) throw new ParserException();
                        bytes.Add(Opcode.Halt.Value());
                        WriteGlobalStackOffset();
                        return bytes;

                    case Token.TokenType.Ident:
                        ParseIdent();
                        break;

                    case Token.TokenType.Builtin:
                        ParseBuiltin();
                        break;

                    case Token.TokenType.ENDL:
                        InFunctionCall = false;
                        break;

                    default:
                        Error($"Unexpected symbol (Line {Current.Line + 1})");
                        break;
                }

                Next(true);
            }
        }
    }

    public sealed class ParserException : Exception
    {
        public ParserException() { }
        public ParserException(string message) : base(message) { }
        public ParserException(string message, Exception innerException) : base(message, innerException) { }
    }

    static class EnumByteExt
    {
        public static byte Value(this Parser.Opcode code ) { return (byte)code; }
        public static byte Value(this Parser.Register reg ) { return (byte)reg; }
    }
}