using System.Diagnostics;

namespace Macroc
{

    internal class Entry
    {
        static void Main(string[] args)
        {
            var argData = ArgHelper.ParseArgs(args);

            var stopwatch = Stopwatch.StartNew();
            Logger.Log("Reading input file...");

            string data = "";
            using (TextReader tr = new StreamReader(args[0]))
            {
                data = tr.ReadToEnd();
            }

            Logger.Log("Lexing input...");
            Lexer lexer = new(ref data);
            var toks = lexer.Lex();

            Logger.Log("Parsing input...");
            Parser parser = new(toks);
            var bytecode = parser.Parse();

            Logger.Verbose("Writing result...");
            using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(Path.GetFileNameWithoutExtension(args[0]) + ".mcc"), System.Text.Encoding.UTF8, false))
            {
                bw.Write(bytecode.ToArray());
            }

            stopwatch.Stop();

            Logger.Log($"Done in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}