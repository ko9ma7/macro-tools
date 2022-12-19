using MacroCommon;

namespace Macro
{
    internal static class ArgHelper
    {
        public struct ArgData
        {
            public string SourceFile { get; set; }
            public string TargetFile { get; set; }
            public bool Compile { get; set; }
            public bool Run { get; set; }
            public bool Debug { get; set; }
            public bool Benchmark { get; set; }
            public bool Verbose { get; set; }
            public bool Quiet { get; set; }
            public int MemorySize { get; set; }
            public ArgData() { SourceFile = ""; TargetFile = ""; Debug = false; Benchmark = false; Compile = true; Run = false; Verbose = false; Quiet = false; MemorySize = 16; }
        }

        public static ArgData ParseArgs(string[] args)
        {
            ArgData data = new ArgData();

            int l = args.Length;
            for (int i = 0; i < l; i++)
            {
                switch (args[i])
                {
                    case "-o":
                        if (i + 1 >= l)
                        {
                            Logger.Error("Expected file name after switch '-o'...");
                            throw new ArgumentException("Expected file name after switch '-o'.");
                        }
                        data.TargetFile = args[++i];
                        break;
                    case "-m":
                        if (i + 1 >= l)
                        {
                            Logger.Error("Expected int after switch '-m'...");
                            throw new ArgumentException("Expected int after switch '-m'.");
                        }
                        int size = 0;
                        if (!Int32.TryParse(args[++i], out size))
                        {
                            Logger.Error("Expected int after switch '-m'...");
                            throw new ArgumentException("Expected int after switch '-m'.");
                        }
                        data.MemorySize = size;
                        break;
                    case "-d":
                        data.Debug = true;
                        break;
                    case "-b":
                        data.Benchmark = true;
                        break;
                    case "-c":
                        data.Compile = true;
                        data.Run = false;
                        break;
                    case "-r":
                        data.Compile = false;
                        data.Run = true;
                        break;
                    case "-v":
                        data.Verbose = true;
                        break;
                    case "-q":
                        data.Quiet = true;
                        break;
                    default:
                        if (args[i][0] == '-')
                        {
                            Logger.Error($"Ignoring unknown switch '{args[i]}'");
                            break;
                        }
                        if (data.SourceFile != "")
                        {
                            Logger.Error("[WARNING] Source file passed in twice...");
                            throw new ArgumentException("Source file passed in twice.");
                        }
                        data.SourceFile = args[i];
                        break;
                }
            }
            
            if (!File.Exists(data.SourceFile))
            {
                Logger.Error($"Source file could not be found...");
                throw new FileNotFoundException("Source file could not be found.");
            }
            if (data.TargetFile == "")
            {
                data.TargetFile = Path.GetFileNameWithoutExtension(data.SourceFile) + ".mcc";
            }

            return data;
        }
    }
}
