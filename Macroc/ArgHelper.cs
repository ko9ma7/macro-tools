using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Macroc
{
    internal static class ArgHelper
    {
        public struct ArgData
        {
            public string SourceFile { get; set; }
            public string TargetFile { get; set; }
            public ArgData() { SourceFile = ""; TargetFile = ""; }
        }

        public static ArgData ParseArgs(string[] args)
        {
            ArgData data = new ArgData();

            int l = args.Length;
            for (int i = 0; i < l; i++)
            {
                switch (args[i])
                {
                    case "-v":
                        Logger.IsVerbose = true;
                        break;
                    case "-o":
                        if (i + 1 >= l)
                        {
                            Logger.Error("Expected file name after switch '-o'...");
                            Environment.Exit((int)ExitCode.ArgError);
                        }
                        data.TargetFile = args[++i];
                        break;
                    default:
                        if (args[i][0] == '-')
                        {
                            Logger.Error($"Ignoring unknown switch '{args[i]}'");
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
            if (data.TargetFile == "")
            {
                data.TargetFile = Path.GetFileNameWithoutExtension(data.SourceFile) + ".mcc";
            }

            return data;
        }
    }
}
