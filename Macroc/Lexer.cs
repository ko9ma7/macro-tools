namespace Macroc
{
    internal sealed class Lexer
    {
        private readonly string Data;
        private int CurPos;
        private char Current;
        private int Line;
        private bool IsValid;
        public Lexer(ref string data)
        {
            Data = data;
            CurPos = -1;
            Current = (char)0;
            Line = 0;
            Next();
            IsValid = true;
        }
        public void Error(string message)
        { 
            Logger.Error(message);
            IsValid = false;
        }

        private char Peek()
        {
            if (CurPos + 1 >= Data.Length)
            {
                Error("Unexpected end of file");
                Environment.Exit((int)ExitCode.LexerError);
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
                    Environment.Exit((int)ExitCode.LexerError);
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

        private List<Token> LexWord()
        {
            List<Token> toks = new();

            if (!char.IsLetterOrDigit(Current) && Current != '"')
            {
                Error($"Error: unknown symbol {Current} (Line {Line + 1})");
                return toks;
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
                return toks;
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
                toks.Add(new IntToken(ival, Line));
                return toks;
            }

            if (float.TryParse(chunk, out float fval))
            {
                toks.Add(new FloatToken(fval, Line));
                return toks;
            }

            switch (chunk)
            {
                case "move":
                    toks.Add(new BuiltinToken(Builtin.Move, Line));
                    break;
                case "drag":
                    toks.Add(new BuiltinToken(Builtin.Drag, Line));
                    break;
                case "click":
                    toks.Add(new BuiltinToken(Builtin.Click, Line));
                    break;
                case "type":
                    toks.Add(new BuiltinToken(Builtin.Type, Line));
                    break;
                case "mod":
                    toks.Add(new BuiltinToken(Builtin.Mod, Line));
                    break;
                case "start":
                    toks.Add(new BuiltinToken(Builtin.Start, Line));
                    break;
                case "end":
                    toks.Add(new BuiltinToken(Builtin.End, Line));
                    break;
                case "int":
                    toks.Add(new BuiltinToken(Builtin.Int, Line));
                    break;
                case "float":
                    toks.Add(new BuiltinToken(Builtin.Float, Line));
                    break;
                default:
                    toks.Add(new IdentToken(chunk, Line));
                    break;
            }
            return toks;
        }

        public List<Token> Lex()
        {
            List<Token> toks = new();

            while (true)
            {
                SkipWhitespace();
                switch (Current)
                {
                    case '\0':
                        if (!IsValid) Environment.Exit((int)ExitCode.LexerError);
                        toks.Add(new EOSToken(Line));
                        return toks;
                    case '\n':
                        toks.Add(new ENDLToken(Line));
                        Line++;
                        break;
                    case '+':
                        toks.Add(new OperatorToken(OperatorType.Add, Line));
                        break;
                    case '-':
                        toks.Add(new OperatorToken(OperatorType.Subtract, Line));
                        break;
                    case '*':
                        toks.Add(new OperatorToken(OperatorType.Multiply, Line));
                        break;
                    case '/':
                        toks.Add(new OperatorToken(OperatorType.Divide, Line));
                        break;
                    case '(':
                        toks.Add(new OperatorToken(OperatorType.LeftParen, Line));
                        break;
                    case ')':
                        toks.Add(new OperatorToken(OperatorType.RightParen, Line));
                        break;
                    case '<':
                        if (Peek() == '-')
                        {
                            toks.Add(new OperatorToken(OperatorType.Assign, Line));
                            Next();
                        }
                        break;
                    default:
                        toks.AddRange(LexWord());
                        break;
                }

                Next(true);
            }
        }
    }
}