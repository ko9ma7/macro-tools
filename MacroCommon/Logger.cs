namespace MacroCommon
{
    public static class Logger
    {
        private static bool IsVerbose { get; set; }
        private static bool IsQuiet { get; set; }
        public static void Log(string message)
        {
            if (!IsQuiet)
            Console.WriteLine($"[INFO] {message}");
        }

        public static void VerboseLog(string message)
        {
            if (IsVerbose && !IsQuiet)
            Console.WriteLine($"[VERBOSE] {message}");
        }

        public static void Error(string message)
        {
            if (IsQuiet) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void SetVerbose(bool verbose)
        {
            IsVerbose = verbose;
        }

        public static void SetQuiet(bool quiet)
        {
            IsQuiet = quiet;
        }
    }
}