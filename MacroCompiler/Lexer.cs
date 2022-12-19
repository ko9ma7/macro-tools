using MacroCommon;

namespace MacroCompiler
{
    public sealed class Lexer
    {
        private string Data;
        private List<Token> toks;
        private int CurPos;
        private char Current;
        private int Line;
        private bool IsValid;
        private bool Ready;
        
        /// <summary>
        /// Constructs a new lexer
        /// </summary>
        public Lexer()
        {
            toks = new();
            CurPos = -1;
            Current = (char)0;
            Line = 0;
            IsValid = true;
            Ready = false;
        }
        
        /// <summary>
        /// Binds the given data to the lexer.
        /// </summary>
        /// <param name="data">The data to bind to.</param>
        public void BindData(ref string data)
        {
            Data = data;
            Next();
            Ready = true;
        }
        private void Error(string message)
        { 
            Logger.Error(message);
            IsValid = false;
        }

        private char Peek()
        {
            if (CurPos + 1 >= Data.Length)
            {
                Error("Unexpected end of file");
                throw new EndOfStreamException();
            }
            return Data[CurPos + 1];
        }

        private void Back()
        {
            CurPos--;
            Current = Data[CurPos];
        }

        private void Next(bool allowEOF = false)
        {
            CurPos++;
            if (CurPos >= Data.Length)
            {
                if (!allowEOF)
                {
                    Error("Unexpected end of file");
                    throw new EndOfStreamException();
                }
                Current = '\0';
                return;
            }
            Current = Data[CurPos];
        }

        private void SkipWhitespace()
        {
            bool loop = true;
            while (loop)
            {
                switch (Current)
                {
                    case '\r':
                    case '\t':
                    case ' ':
                        break;
                    default:
                        loop = false;
                        break;
                }
                if (loop) Next(true);
            }
        }

        private void LexWord()
        {
            if (!char.IsLetterOrDigit(Current) && Current != '"')
            {
                Error($"Error: unknown symbol {Current} (Line {Line + 1})");
                return;
            }
            
            // If there is a quote, lex and push string
            string chunk = "";
            if (Current == '"')
            {
                Next();
                while (Current != '"')
                {
                    chunk += Current;
                    Next(true);
                }
                toks.Add(new StringToken(chunk, Line));
                return;
            }
            
            // Get letters/numbers until space
            while (char.IsLetterOrDigit(Current) || Current == '.' || Current == '_')
            {
                chunk += Current;
                Next(true);
            }

            Back();
            
            // Check if it is an int or float
            if (int.TryParse(chunk, out int ival))
            {
                toks.Add(new IntLiteral(ival, Line));
                return;
            }

            if (float.TryParse(chunk, out float fval))
            {
                toks.Add(new FloatLiteral(fval, Line));
                return;
            }

            switch (chunk)
            {
                case "move":
                    toks.Add(new BuiltinToken(Token.BuiltinType.Move, Line));
                    break;
                case "drag":
                    toks.Add(new BuiltinToken(Token.BuiltinType.Drag, Line));
                    break;
                case "click":
                    toks.Add(new BuiltinToken(Token.BuiltinType.Click, Line));
                    break;
                case "type":
                    toks.Add(new BuiltinToken(Token.BuiltinType.Type, Line));
                    break;
                case "mod":
                    toks.Add(new BuiltinToken(Token.BuiltinType.Mod, Line));
                    break;
                case "start":
                    toks.Add(new BuiltinToken(Token.BuiltinType.Start, Line));
                    break;
                case "end":
                    toks.Add(new BuiltinToken(Token.BuiltinType.End, Line));
                    break;
                case "int":
                    toks.Add(new BuiltinToken(Token.BuiltinType.Int, Line));
                    break;
                case "float":
                    toks.Add(new BuiltinToken(Token.BuiltinType.Float, Line));
                    break;
                case "delay":
                    toks.Add(new BuiltinToken(Token.BuiltinType.Delay, Line));
                    break;
                case "display":
                    toks.Add(new BuiltinToken(Token.BuiltinType.Display, Line));
                    break;
                default:
                    toks.Add(new IdentToken(chunk, Line));
                    break;
            }
        }

        public List<Token> Lex()
        {
            if (!Ready)
            {
                Logger.Error("No data bound to lexer.");
                throw new LexerException("No data bound to lexer.");
            }

            while (true)
            {
                SkipWhitespace();
                switch (Current)
                {
                    case '\0':
                        if (!IsValid) throw new LexerException();
                        toks.Add(new EOSToken(Line));
                        return toks;
                    case '\n':
                        toks.Add(new ENDLToken(Line));
                        Line++;
                        break;
                    case '+':
                        toks.Add(new OperatorToken(Token.OperatorType.Add, Line));
                        break;
                    case '-':
                        toks.Add(new OperatorToken(Token.OperatorType.Subtract, Line));
                        break;
                    case '*':
                        toks.Add(new OperatorToken(Token.OperatorType.Multiply, Line));
                        break;
                    case '/':
                        toks.Add(new OperatorToken(Token.OperatorType.Divide, Line));
                        break;
                    case '(':
                        toks.Add(new OperatorToken(Token.OperatorType.LeftParen, Line));
                        break;
                    case ')':
                        toks.Add(new OperatorToken(Token.OperatorType.RightParen, Line));
                        break;
                    case '<':
                        if (Peek() == '-')
                        {
                            toks.Add(new OperatorToken(Token.OperatorType.Assign, Line));
                            Next();
                        }
                        break;
                    default:
                        LexWord();
                        break;
                }

                Next(true);
            }
        }
    }

    public sealed class LexerException : Exception
    { 
        public LexerException() { }
        public LexerException(string message) : base(message) { }
        public LexerException(string message, Exception innerException) : base(message, innerException) { }
    }
}