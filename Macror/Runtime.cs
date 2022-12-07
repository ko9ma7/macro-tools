namespace Macror
{
    internal sealed class Runtime
    {
        private byte[] Program;
        private byte[] Memory;
        private Stack Stack;
        public Runtime(ref byte[] program, int memorySize)
        {
            Program = program;
            Memory = new byte[memorySize];
            Stack = new(ref Memory);
        }

    }
}