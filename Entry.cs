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
            using (TextReader tr = new StreamReader(args[0]))
            {
                data = tr.ReadToEnd();
            }

            Lexer lexer = new(ref data);

            var toks = lexer.Lex();
        }
    }
}