using BenchmarkDotNet.Running;
using System.Diagnostics;

namespace Macroc
{

    internal class Entry
    {
        internal static ArgHelper.ArgData Args { get; set; }
        static void Main(string[] args)
        {
            Args = ArgHelper.ParseArgs(args);

            if (Args.Benchmark)
            {
                BenchmarkRunner.Run<Benchmark>();
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            string data = "";
            using (TextReader tr = new StreamReader(Args.SourceFile))
            {
                data = tr.ReadToEnd();
            }
                
            stopwatch.Stop();
            Logger.Log("Lexing input...");
            stopwatch.Start();
            Lexer lexer = new(ref data);
            var toks = lexer.Lex();

            stopwatch.Stop();
            Logger.Log("Parsing input...");
            stopwatch.Start();
            Parser parser = new(toks);
            var bytecode = parser.Parse();

            stopwatch.Stop();
            using (BinaryWriter bw = new(File.OpenWrite(Args.TargetFile), System.Text.Encoding.UTF8, false))
            {
                bw.Write(bytecode.ToArray());
            }

            Logger.Log($"Done in {stopwatch.ElapsedMilliseconds}ms");

            if (Args.Debug)
            {
                Logger.Log($"Tokens: | {DebugInfo(toks)}");
                Logger.Log($"Bytes: | {DebugInfo(bytecode)}");
                Logger.Log($"Characters per token: {data.Length / (float)toks.Count}");
                Logger.Log($"Bytes per Token: {bytecode.Count / (float)toks.Count}");
            }
        }

        private static string DebugInfo<T>(List<T> list)
        {
            return $"Count: {list.Count} | Capacity : {list.Capacity} | Wasted: {(list.Capacity - list.Count) / (float)list.Capacity * 100f}% ({list.Capacity - list.Count})";
        }
    }
}