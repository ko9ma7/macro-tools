using MacroCommon;
using MacroCompiler;
using MacroRuntime;

namespace Macro
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var Args = ArgHelper.ParseArgs(args);
            Logger.SetQuiet(Args.Quiet);
            Logger.SetVerbose(Args.Verbose);

            if (Args.Compile)
            {
                Compiler compiler = new(new Compiler.CompilerFlags(Args.Debug));
                string data = File.ReadAllText(Args.SourceFile);
                var compiled = compiler.Compile(ref data);
                File.WriteAllBytes(Args.TargetFile, compiled.ToArray());
            }
            else if (Args.Run)
            {
                byte[] program = File.ReadAllBytes(Args.SourceFile);
                Runtime runtime = new(ref program, Args.MemorySize);
                runtime.Run();
            }
        }
    }
}