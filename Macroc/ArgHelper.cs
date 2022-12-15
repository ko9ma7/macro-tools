namespace Macroc
{
    internal static class ArgHelper
    {
        public struct ArgData
        {
            public string SourceFile { get; set; }
            public string TargetFile { get; set; }
            public bool Debug { get; set; }
            public bool Benchmark { get; set; }
            public ArgData() { SourceFile = ""; TargetFile = ""; Debug = false; Benchmark = false; }
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
                            Environment.Exit((int)ExitCode.ArgError);
                        }
                        data.TargetFile = args[++i];
                        break;
                    case "-d":
                        data.Debug = true;
                        break;
                    case "-b":
                        data.Benchmark = true;
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
                            Environment.Exit((int)ExitCode.ArgError);
                        }
                        data.SourceFile = args[i];
                        break;
                }
            }
            
            if (!File.Exists(data.SourceFile) && !data.Benchmark)
            {
                Logger.Error($"Source file could not be found...");
                Environment.Exit((int)ExitCode.NoInputFile);
            }
            if (data.TargetFile == "")
            {
                data.TargetFile = Path.GetFileNameWithoutExtension(data.SourceFile) + ".mcc";
            }

            return data;
        }
    }
}
