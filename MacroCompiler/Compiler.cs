using System.Diagnostics;
using MacroCommon;

namespace MacroCompiler
{
    public sealed class Compiler
    {
        internal Lexer Lexer { get; }
        internal Parser Parser { get; }
        private CompilerFlags Flags { get; }

        public sealed class CompilerFlags
        {
            public bool DisplayDebugInfo { get; }
            public CompilerFlags(bool displayDebugInfo)
            {
                DisplayDebugInfo = displayDebugInfo;
            }
        }

        public Compiler(CompilerFlags flags)
        {
            Flags = flags;

            Lexer = new();
            Parser = new();
        }

        public List<byte> Compile(ref string data)
        {
            var stopwatch = Stopwatch.StartNew();

            stopwatch.Stop();
            Logger.Log("Lexing input...");
            stopwatch.Start();

            Lexer.BindData(ref data);
            var toks = Lexer.Lex();

            stopwatch.Stop();
            Logger.Log("Parsing input...");
            stopwatch.Start();

            Parser.BindData(toks);
            var bytecode = Parser.Parse();

            stopwatch.Stop();

            Logger.Log($"Done in {stopwatch.ElapsedMilliseconds}ms");

            if (Flags.DisplayDebugInfo) PrintDebugInfo(toks, bytecode, data.Length);

            return bytecode;
        }
        internal void PrintDebugInfo(List<Token> toks, List<byte> bytecode, int dataLength)
        {
            Logger.Log($"Tokens: | {GetDebugString(toks)}");
            Logger.Log($"Bytes: | {GetDebugString(bytecode)}");
            Logger.Log($"Characters per token: {dataLength / (float)toks.Count}");
            Logger.Log($"Bytes per Token: {bytecode.Count / (float)toks.Count}");
        }

        private string GetDebugString<T>(List<T> list)
        {
            return $"Count: {list.Count} | Capacity : {list.Capacity} | Wasted: {(list.Capacity - list.Count) / (float)list.Capacity * 100f}% ({list.Capacity - list.Count})";
        }
    }
}
