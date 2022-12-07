namespace Macror
{
    internal static class ArgHelper
    {
        internal struct ArgData
        {
            public string SourceFile { get; set; }
            public ArgData() { SourceFile = ""; }
        }

        public static ArgData ParseArgs(string[] args)
        {
            ArgData data = new ArgData();

            int l = args.Length;
            for (int i = 0; i < l; i++)
            {
                switch (args[i])
                {
                    default:
                        if (args[i][0] == '-')
                        {
                            Logger.Error($"Ignoring unknown switch '{args[i]}'");
                            i++;
                            break;
                        }
                        data.SourceFile = args[i];
                        break;
                }
            }
            
            if (!File.Exists(data.SourceFile))
            {
                Logger.Error($"Source file could not be found...");
                Environment.Exit((int)ExitCode.NoInputFile);
            }

            return data;
        }
    }
}