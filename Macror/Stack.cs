namespace Macror
{
        internal sealed class Stack
        {
            private byte[] Memory;
            public int StackPointer { get; private set; }
            public int BasePointer { get; private set; }
            /// <summary>
            /// Creates a stack at the top of the given byte array
            /// </summary>
            public Stack(ref byte[] memory)
            {
                Memory = memory;
                StackPointer = Memory.Length;
                BasePointer = StackPointer;
            }

            public void SetupFrame(int size)
            {
                Push(BasePointer);
                BasePointer = StackPointer;
                StackPointer -= size;
            }

            public void PopFrame()
            {
                StackPointer = BasePointer;
                BasePointer = PopInt();
            }

            public int PopInt()
            {
                byte[] bytes = new byte[4];
                Array.Copy(Memory, StackPointer, bytes, 0, 4);
                StackPointer += 4;
                return BitConverter.ToInt32(bytes);
            }

            public float PopFloat()
            {
                byte[] bytes = new byte[4];
                Array.Copy(Memory, StackPointer, bytes, 0, 4);
                StackPointer += 4;
                return BitConverter.ToSingle(bytes);
            }

            public void Push(int value)
            {
                StackPointer -= 4;
                Array.Copy(BitConverter.GetBytes(value), 0, Memory, StackPointer, 4);
            }

            public void Push(byte[] bytes)
            {
                StackPointer -= 4;
                Array.Copy(bytes, 0, Memory, StackPointer, 4);
            }
        }
}