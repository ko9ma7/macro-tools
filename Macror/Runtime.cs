namespace Macror
{
    internal sealed class Runtime
    {
        private byte[] Program;
        private byte[] Memory;
        private Stack Stack;
        private OperationMode Mode;
        private int InstructionPointer;
        private bool Running;
        public Runtime(ref byte[] program, int memorySize)
        {
            Program = program;
            Memory = new byte[memorySize];
            Stack = new(ref Memory);
            InstructionPointer = 0;
            Running = true;
        }

        private void Add()
        {
            byte[] bytes;
            if (Mode == OperationMode.Int)
            {
                int right = Stack.PopInt();
                int left = Stack.PopInt();
                bytes = BitConverter.GetBytes(left + right);
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left + right);
            }
            Stack.Push(bytes);
        }

        private void Subtract()
        {
            byte[] bytes;
            if (Mode == OperationMode.Int)
            {
                int right = Stack.PopInt();
                int left = Stack.PopInt();
                bytes = BitConverter.GetBytes(left - right);
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left - right);
            }
            Stack.Push(bytes);
        }

        private void Multiply()
        {
            byte[] bytes;
            if (Mode == OperationMode.Int)
            {
                int right = Stack.PopInt();
                int left = Stack.PopInt();
                bytes = BitConverter.GetBytes(left * right);
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left * right);
            }
            Stack.Push(bytes);
        }

        private void Divide()
        {
            byte[] bytes;
            if (Mode == OperationMode.Int)
            {
                int right = Stack.PopInt();
                int left = Stack.PopInt();
                bytes = BitConverter.GetBytes(left / right);
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left / right);
            }
            Stack.Push(bytes);
        }

        private void StoreVariable()
        {

        }

        public void Run()
        {
            while (Running)
            {
                switch ((Opcode)Memory[InstructionPointer])
                {
                    case Opcode.Halt:
                        Running = false;
                        break;

                    case Opcode.IntOperation:
                        Mode = OperationMode.Int;
                        break;

                    case Opcode.FloatOperation:
                        Mode = OperationMode.Float;
                        break;

                    case Opcode.PushImm:
                    {
                        byte[] bytes = new byte[4];
                        Array.Copy(Program, ++InstructionPointer, bytes, 0, 4);
                        Stack.Push(bytes);
                        break;
                    }

                    case Opcode.Add:
                        Add();
                        break;

                    case Opcode.Sub:
                        Subtract();
                        break;

                    case Opcode.Mult:
                        Multiply();
                        break;
                    
                    case Opcode.Div:
                        Divide();
                        break;

                    case Opcode.StoreVar:
                        StoreVariable();
                        break;
                }   

                InstructionPointer++;
            }
        }

        public enum Opcode
        {
            LDVar = 0x00,
            LDImm,
            PushVar,
            PushImm,
            StoreVar,
            FloatOperation,
            IntOperation,
            Call,
            StartFunc,
            EndFunc,
            Halt,
            Add = 0x10,
            Sub,
            Mult,
            Div,
            SetupFrame = 0x20,
            FreeFrame,
        }

        public enum OperationMode
        {
            Int = 0x00,
            Float
        }
    }
}