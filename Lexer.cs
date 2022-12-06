namespace Macroc
{
    internal sealed class Lexer
    {
        private string Data;
        private int CurPos;
        private char Current;
        private int Line;
        public Lexer(ref string data)
        {
            Data = data;
            CurPos = -1;
            Current = (char)0;
            Line = 0;
            Next();
        }

        private char Peek()
        {
            if (CurPos + 1 >= Data.Length)
            {
                Console.WriteLine("Unexpected end of file");
                Environment.Exit(-2);
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
                    Console.WriteLine("Unexpected end of file");
                    Environment.Exit(-2);
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
                    case '\n':
                        Line++;
                        break;
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

        public List<Token> Lex()
        {
            List<Token> toks = new();

            while (true)
            {
                SkipWhitespace();
                switch (Current)
                {
                    case '\0':
                        toks.Add(new EOSToken(Line));
                        return toks;
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
                    case '<':
                        if (Peek() == '-')
                        {
                            toks.Add(new OperatorToken(OperatorType.Assign, Line));
                            Next();
                        }
                        break;
                    default:
                    {
                        if (!char.IsLetterOrDigit(Current) && Current != '"')
                        {
                            Console.WriteLine($"Error: unknown symbol {Current} (Line {Line + 1})");
                            Environment.Exit(-3);
                        }

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
                            break;
                        }

                        while (char.IsLetterOrDigit(Current) || Current == '.' || Current == '_')
                        {
                            chunk += Current;
                            Next(true);
                        }

                        Back();

                        if (int.TryParse(chunk, out int ival))
                        {
                            toks.Add(new IntToken(ival, Line));
                            break;
                        }

                        if (float.TryParse(chunk, out float fval))
                        {
                            toks.Add(new FloatToken(fval, Line));
                            break;
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

                        break;
                    }
                }

                Next(true);
            }
        }
    }
}