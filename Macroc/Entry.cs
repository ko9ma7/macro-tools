using System.Diagnostics;

namespace Macroc
{

    internal class Entry
    {
        static void Main(string[] args)
        {
            var argData = ArgHelper.ParseArgs(args);

            var stopwatch = Stopwatch.StartNew();

            string data = "";
            using (TextReader tr = new StreamReader(argData.SourceFile))
            {
                data = tr.ReadToEnd();
            }

            Logger.Log("Lexing input...");
            Lexer lexer = new(ref data);
            var toks = lexer.Lex();

            Logger.Log("Parsing input...");
            Parser parser = new(toks);
            var bytecode = parser.Parse();

            using (BinaryWriter bw = new(File.OpenWrite(argData.TargetFile), System.Text.Encoding.UTF8, false))
            {
                bw.Write(bytecode.ToArray());
            }

            stopwatch.Stop();

            Logger.Log($"Done in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}