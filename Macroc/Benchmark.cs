using BenchmarkDotNet.Attributes;

namespace Macroc
{
    [MemoryDiagnoser]
    public class Benchmark
    {
        static string data = "";

        [GlobalSetup]
        public void Setup()
        {
            data = File.ReadAllText("./test.mcs");
        }

        [Benchmark]
        public void LexAndParse()
        {
            Lexer lexer = new Lexer(ref data);
            var toks = lexer.Lex();

            Parser parser = new Parser(toks);
            var bytes = parser.Parse();
        }
    }
}
