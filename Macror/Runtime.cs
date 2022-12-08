using System.ComponentModel.DataAnnotations;

namespace Macror
{
    internal sealed class Runtime
    {
        private readonly byte[] Program;
        private readonly byte[] Memory;
        private readonly Stack Stack;
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

        private int GetIntFromPrgm()
        {
            int address = BitConverter.ToInt32(Program, InstructionPointer);
            InstructionPointer += 3;
            return address;
        }

        private byte[] GetBytesFromPrgm()
        {
            byte[] bytes = new byte[4];
            Array.Copy(Program, InstructionPointer, bytes, 0, 4);
            InstructionPointer += 3;
            return bytes;
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

        private void PushImm()
        {
            ++InstructionPointer;
            Stack.Push(GetBytesFromPrgm());
        }
        private void PushVar()
        {
            ++InstructionPointer;
            int address = GetIntFromPrgm();
            int val = Stack.ReadOffsetInt(address);
            Stack.Push(val);
        }

        private void StoreVariable()
        {
            ++InstructionPointer;
            int address = GetIntFromPrgm();
            Stack.WriteToOffset(Stack.PopBytes(), address);
        }

        private void SetupFrame()
        {
            ++InstructionPointer;
            int size = GetIntFromPrgm();
            Stack.SetupFrame(size);
        }

        private void Display()
        {
            ++InstructionPointer;
            int address = GetIntFromPrgm();
            Console.Write($"Offset: {address} | ");

            switch (Mode)
            {
                case OperationMode.Int:
                    Console.WriteLine($"Value: {Stack.ReadOffsetInt(address)}");
                    break;

                case OperationMode.Float:
                    Console.WriteLine($"Value: {Stack.ReadOffsetFloat(address)}");
                    break;
            }
        }

        public void Run()
        {
            while (Running)
            {
                Opcode opcode = (Opcode)Program[InstructionPointer];
                switch (opcode)
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

                    case Opcode.SetupFrame:
                        SetupFrame();
                        break;

                    case Opcode.FreeFrame:
                        Stack.PopFrame();
                        break;

                    case Opcode.PushImm:
                        PushImm();
                        break;

                    case Opcode.PushVar:
                        PushVar();
                        break;

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

                    case Opcode.Display:
                        Display();
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
            Display,
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