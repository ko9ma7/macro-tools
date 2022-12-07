using System.ComponentModel;

namespace Macroc
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
            Add = 0x10,
            Sub,
            Mult,
            Div,
            SetupFrame = 0x20,
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

        private readonly List<Token> Toks;
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
        private readonly Dictionary<string, VarEntry> StackVarTable;
        private readonly Dictionary<string, VarEntry> GlobalVarTable;
        private Dictionary<string, VarEntry> VarTable { get => InFunction ? StackVarTable : GlobalVarTable; }
        private readonly Dictionary<string, int> FuncTable;
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
            Logger.Error(message);
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
                    Environment.Exit((int)ExitCode.ParserError);
                }
                Current = new EOSToken(0);
                return;
            }
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
                Error($"Generic assign is not valid (Line {Current.Line + 1})");
                return 0xFF;

                default:
                return 0xFF;
            }
        }

        private static int GetOperatorPrecedence(OperatorType op) =>
            op switch
            {
                OperatorType.LeftParen => 5,
                OperatorType.Multiply or OperatorType.Divide => 3,
                OperatorType.Add or OperatorType.Subtract => 2,
                _ => 0
            };

        private List<byte> ParseExprIntLit(VarType type, ref bool expectOperator)
        {
            List<byte> bytes = new(5);
            if (expectOperator)
            {
                Error($"Expected operator (Line {Current.Line + 1})");
                return bytes;
            }

            bytes.Add(Opcode.PushImm.Value());
            int value = ((IntToken)Current).Value;

            // if expression is float-typed, push float. otherwise, push int
            if (type == VarType.Int) bytes.AddRange(BitConverter.GetBytes(value));
            else bytes.AddRange(BitConverter.GetBytes((float)value));

            expectOperator = true;

            return bytes;
        }
        private List<byte> ParseExprFloatLit(VarType type, ref bool expectOperator)
        {
            List<byte> bytes = new(5);
            if (expectOperator)
            {
                Error($"Expected operator (Line {Current.Line + 1})");
                return bytes;
            }

            bytes.Add(Opcode.PushImm.Value());
            float value = ((FloatToken)Current).Value;

            // if expression is float-typed, push float. otherwise, push int
            if (type == VarType.Float) bytes.AddRange(BitConverter.GetBytes(value));
            else bytes.AddRange(BitConverter.GetBytes((int)value));

            expectOperator = true;

            return bytes;
        }

        private List<byte> ParseExprIdent(VarType type, ref bool expectOperator)
        {
            List<byte> bytes = new(5);
            if (expectOperator)
            {
                Error($"Expected operator (Line {Current.Line + 1})");
                return bytes;
            }


            IdentToken identToken = (IdentToken)Current;
            if (!AssertVarExists(identToken)) return bytes;

            bytes.Add(Opcode.PushVar.Value());
            bytes.AddRange(BitConverter.GetBytes(VarTable[identToken.Ident].Offset));

            expectOperator = true;

            return bytes;
        }

        private List<byte> ParseExprOperator(Stack<OperatorType> operatorStack, ref bool expectOperator)
        {
            List<byte> bytes = new();
            OperatorType operation = ((OperatorToken)Current).Operator;
            // if operator stack is empty, we must push
            if (operatorStack.Count == 0)
            {
                operatorStack.Push(operation);
                expectOperator = false;
                return bytes;
            }

            int currentPrec = GetOperatorPrecedence(operation);
            int prevPrec = GetOperatorPrecedence(operatorStack.Peek());

            if (operation == OperatorType.LeftParen)
            {
                // Left paren must take place of value
                if (!expectOperator)
                {
                    Error($"Expected value, got left parenthesis (Line {Current.Line + 1})");
                    return bytes;
                }

                operatorStack.Push(operation);
                return bytes;
            }

            if (operation == OperatorType.RightParen)
            {
                // Right paren must take place of operator
                if (expectOperator)
                {
                    Error($"Expected operator, got right parenthesis (Line {Current.Line + 1})");
                    return bytes;
                }
                
                // loop until we find a left parenthesis
                while (operatorStack.Peek() != OperatorType.LeftParen)
                {
                    OperatorType op = operatorStack.Pop();
                    bytes.Add(GetOperatorOpcode(op));

                    // if stack is empty, then we haven't found a matching parenthesis
                    // this indicates unbalanced parentheses
                    if (operatorStack.Count == 0)
                    {
                        Error($"Unbalanced parentheses (Line {Current.Line + 1})");
                        return bytes;
                    }
                }
                return bytes;
            }

            expectOperator = false;

            // is precedences are less-than or equal, pop previous and push current
            if (currentPrec <= prevPrec)
            {
                OperatorType op = operatorStack.Pop();
                bytes.Add(GetOperatorOpcode(op));
                operatorStack.Push(operation);
                return bytes;
            }
            // if current has greater precedence
            else
            {
                operatorStack.Push(operation);
                return bytes;
            }
        }


        private List<byte> ParseExpression(VarType type)
        {
            List<byte> bytes = new();
            Stack<OperatorType> operatorStack = new();
            bool expectOperator = false;

            Next();

            while (Current.Type != TokenType.ENDL && Current.Type != TokenType.EOS)
            {
                if (expectOperator && Current.Type != TokenType.Operator)
                {
                    Error($"Expected operator (Line {Current.Line + 1})");
                    return bytes;
                }

                switch (Current.Type)
                {
                    case TokenType.Int:
                        bytes.AddRange(ParseExprIntLit(type, ref expectOperator));
                        break;

                    case TokenType.Float:
                        bytes.AddRange(ParseExprFloatLit(type, ref expectOperator));
                        break;

                    case TokenType.Ident:
                        bytes.AddRange(ParseExprIdent(type, ref expectOperator));
                        break;

                    case TokenType.Operator:
                        bytes.AddRange(ParseExprOperator(operatorStack, ref expectOperator));
                        break;

                    default:
                        Error($"Invalid term in expression (Line {Current.Line + 1})");
                        return bytes;
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

            return bytes;
        }

        /// <summary>
        /// Parse an expression beginning with an ident
        /// <summary>
        private List<byte> ParseIdent()
        {
            List<byte> bytes = new();
            if (PeekType() == TokenType.Operator)
            {
                // Must be an assignment
                // Store the asignee
                string assignee = ((IdentToken)Current).Ident;
                if (!AssertVarExists(assignee)) return bytes;
                Next();

                // Make sure we are assigning
                if (((OperatorToken)Current).Operator != OperatorType.Assign)
                {
                    Error($"Expected assignment operator (Line {Current.Line + 1}) ");
                    return bytes;
                }

                VarType resultType = VarTable[assignee].Type;

                // Write operation type
                bytes.Add(((resultType == VarType.Int ? Opcode.IntOperation : Opcode.FloatOperation)).Value());

                bytes.AddRange(ParseExpression(resultType));

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

        /// <summary>
        /// Parse a builtin statement
        /// </summary>

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
                        Error($"Cannot define function in function (Line {Current.Line + 1})");
                        break;
                    }

                    // Validate token types
                    if (PeekType() != TokenType.Ident)
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

                case Builtin.End:
                    // Only allow end at end of function
                    if (!InFunction)
                    {
                        Error($"'End' only valid at end of function (Line {Current.Line + 1})");
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

                case Builtin.Float:
                {
                    // Must be a variable definition
                    // Validate token types
                    if (PeekType() != TokenType.Ident)
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

                default:
                break;
            }

            return bytes;
        }

        /// <summary>
        /// Parse the data currently bound to the parser
        ///</summary>
        public List<byte> Parse()
        {
            List<byte> bytes = new();

            while (true)
            {
                switch (Current.Type)
                {
                    case TokenType.EOS:
                        if (!IsValid) Environment.Exit((int)ExitCode.ParserError);
                        return bytes;

                    case TokenType.Ident:
                        bytes.AddRange(ParseIdent());
                        break;

                    case TokenType.Builtin:
                        bytes.AddRange(ParseBuiltin());
                        break;

                    case TokenType.ENDL:
                        break;

                    default:
                        break;
                }

                Next(true);
            }
        }
    }

    static class EnumByteExt
    {
        public static byte Value(this Parser.Opcode code ) { return (byte)code; }
        public static byte Value(this Parser.Register reg ) { return (byte)reg; }
    }
}