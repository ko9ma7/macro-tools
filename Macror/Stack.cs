namespace Macror
{
        internal sealed class Stack
        {
            private byte[] Memory;
            public int StackPointer { get; private set; }
            /// <summary>
            /// Creates a stack at the top of the given byte array
            /// </summary>
            public Stack(ref byte[] memory)
            {
                Memory = memory;
                StackPointer = Memory.Length;
            }

            public int PopInt()
            {
            }

            public float PopFloat()
            {

            }
        }
}