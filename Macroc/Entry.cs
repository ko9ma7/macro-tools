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
            using (BinaryWriter bw = new(File.OpenWrite(argData.TargetFile), System.Text.Encoding.UTF8, false))
            {
                bw.Write(bytecode.ToArray());
            }


            Logger.Log($"Done in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}