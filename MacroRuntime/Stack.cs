namespace MacroRuntime
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

        public byte[] PopBytes()
        {

            byte[] bytes = new byte[4];
            Array.Copy(Memory, StackPointer, bytes, 0, 4);
            StackPointer += 4;
            return bytes;
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

        public void WriteToOffset(int value, int offset)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, Memory, BasePointer-offset, 4);
        }
        public void WriteToOffset(float value, int offset)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, Memory, BasePointer-offset, 4);
        }
        public void WriteToOffset(byte[] bytes, int offset)
        {
            Array.Copy(bytes, 0, Memory, BasePointer-offset, 4);
        }

        public int ReadOffsetInt(int offset)
        {
            byte[] bytes = new byte[4];
            Array.Copy(Memory, BasePointer - offset, bytes, 0, 4);
            return BitConverter.ToInt32(bytes);
        }

        public float ReadOffsetFloat(int offset)
        {
            byte[] bytes = new byte[4];
            Array.Copy(Memory, BasePointer - offset, bytes, 0, 4);
            return BitConverter.ToSingle(bytes);
        }

        public byte[] ReadOffsetBytes(int offset)
        {
            byte[] bytes = new byte[4];
            Array.Copy(Memory, BasePointer - offset, bytes, 0, 4);
            return bytes;
        }
    }
}