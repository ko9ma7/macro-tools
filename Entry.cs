namespace Macroc
{

    internal class Entry
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine($"Usage: macroc.exe [path_to_macro]");
                Environment.Exit(-1);
            }

            string data = "";
            if (!File.Exists(args[0]))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fatal Error: file {args[0]} could not be found...");
                Console.ForegroundColor = ConsoleColor.White;
                Environment.Exit(1);
            }

            DateTime start = DateTime.Now;

            using (TextReader tr = new StreamReader(args[0]))
            {
                data = tr.ReadToEnd();
            }

            Console.WriteLine("Lexing input...");
            Lexer lexer = new(ref data);
            var toks = lexer.Lex();

            Console.WriteLine("Parsing input...");
            Parser parser = new(toks);
            var bytecode = parser.Parse();

            using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(Path.GetFileNameWithoutExtension(args[0]) + ".mcc"), System.Text.Encoding.UTF8, false))
            {
                bw.Write(bytecode.ToArray());
            }

            Console.WriteLine($"Done in {(DateTime.Now - start).TotalMilliseconds}ms");
        }
    }
}