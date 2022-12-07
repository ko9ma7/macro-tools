namespace Macror
{
    internal class Entry
    {
        static void Main(string[] args)
        {
            var argData = ArgHelper.ParseArgs(args);

            byte[] program;
            using (BinaryReader br = new BinaryReader(File.OpenRead(argData.SourceFile)))
            {
                int l = (int)br.BaseStream.Length;
                program = br.ReadBytes(l);
            }

            Runtime runtime = new Runtime(ref program, 1000 * 1000);
            runtime.Run();
        }
    }
}