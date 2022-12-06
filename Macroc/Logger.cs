using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Macroc
{
    internal static class Logger
    {
        public static bool IsVerbose = false;

        public static void Log(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        public static void Empty()
        {
            Console.Write('\n');
        }

        public static void Verbose(string message)
        {
            if (IsVerbose) Console.WriteLine($"[VERBOSE] {message}");
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
