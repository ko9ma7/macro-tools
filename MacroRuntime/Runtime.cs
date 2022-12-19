using MacroCommon;

namespace MacroRuntime
{
    public sealed class Runtime
    {
        public const int Version = 1;
        private readonly byte[] Memory;
        private readonly Stack Stack;
        private OperationMode Mode;
        private int InstructionPointer;
        private bool Running;
        /// <summary>
        /// Constructs a new runtime bound to the given program with the given memory size
        /// </summary>
        /// <param name="program">The program to bind to</param>
        /// <param name="memorySize">The amount of memory to allocate in kilobytes</param>
        public Runtime(ref byte[] program, int memorySize)
        {
            if (program.Length < 4)
            {
                Logger.Error($"Invalid Program...");
                throw new RuntimeException("invalid program");
            }

            int progVersion = BitConverter.ToInt32(program, 0);
            if (Version != progVersion)
            {
                Logger.Error($"Parser version is {Version} but bytecode version is {progVersion}...");
                throw new RuntimeException("Invalid bytecode version");
            }

            if (memorySize * 1024 < program.Length)
            {
                Logger.Error("Program does not fit in memory");
                throw new RuntimeException("Program does not fit in memory");
            }

            Memory = new byte[memorySize * 1024];
            Array.Copy(program, Memory, program.Length);
            Stack = new(ref Memory);
            InstructionPointer = 0;
            Running = true;
        }

        private string BytesToFriendly(byte[] bytes)
        {
            return (Mode == OperationMode.Int ? BitConverter.ToInt32(bytes) : BitConverter.ToSingle(bytes)).ToString();
        }

        private int GetIntFromPrgm()
        {
            int address = BitConverter.ToInt32(Memory, InstructionPointer);
            InstructionPointer += 3;
            return address;
        }

        private byte[] GetBytesFromPrgm()
        {
            byte[] bytes = new byte[4];
            Array.Copy(Memory, InstructionPointer, bytes, 0, 4);
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
                Logger.VerboseLog($"Add | {left}+{right}");
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left + right);
                Logger.VerboseLog($"Add | {left}+{right}");
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
                Logger.VerboseLog($"Subtract | {left}-{right}");
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left - right);
                Logger.VerboseLog($"Subtract | {left}-{right}");
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
                Logger.VerboseLog($"Multiply | {left}*{right}");
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left * right);
                Logger.VerboseLog($"Multiply | {left}*{right}");
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
                Logger.VerboseLog($"Divide | {left}/{right}");
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left / right);
                Logger.VerboseLog($"Divide | {left}/{right}");
            }
            Stack.Push(bytes);
        }

        private void PushImm()
        {
            ++InstructionPointer;
            byte[] val = GetBytesFromPrgm();
            Stack.Push(val);
            Logger.VerboseLog($"PushImm | Value: {BytesToFriendly(val)}");
        }
        private void PushVar()
        {
            ++InstructionPointer;
            int address = GetIntFromPrgm();
            byte[] val = Stack.ReadOffsetBytes(address);
            Stack.Push(val);
            Logger.VerboseLog($"PushVar | Value: {BytesToFriendly(val)} | address: {address}");
        }

        private void StoreVariable()
        {
            ++InstructionPointer;
            int address = GetIntFromPrgm();
            byte[] bytes = Stack.PopBytes();
            Stack.WriteToOffset(bytes, address);
            Logger.VerboseLog($"StoreVar | Value: {BytesToFriendly(bytes)} | address: {address}");
        }

        private void SetupFrame()
        {
            ++InstructionPointer;
            int size = GetIntFromPrgm();
            Stack.SetupFrame(size);
            Logger.VerboseLog($"SetupFrame | size: {size}");
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

        private void Move()
        {
            int y = Stack.PopInt();
            int x = Stack.PopInt();
            Logger.VerboseLog($"Move | x: {x} | y: {y}");
            // Move here
        }

        private void Delay()
        {
            float timeInSeconds = Stack.PopFloat();
            Logger.VerboseLog($"Delay | time: {timeInSeconds}");
            Thread.Sleep((int)(timeInSeconds * 1000));
        }

        public void Run()
        {
            while (Running)
            {
                Opcode opcode = (Opcode)Memory[InstructionPointer];
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

                    case Opcode.Move:
                        Move();
                        break;

                    case Opcode.Delay:
                        Delay();
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
            Move,
            Delay,
            Add = 0x20,
            Sub,
            Mult,
            Div,
            SetupFrame = 0x40,
            FreeFrame,
        }

        public enum OperationMode
        {
            Int = 0x00,
            Float
        }

        public sealed class RuntimeException : Exception
        {
            public RuntimeException() { }
            public RuntimeException(string message) : base(message) { }
            public RuntimeException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}