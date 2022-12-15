namespace Macror
{
    internal sealed class Runtime
    {
        public const int Version = 1;
        private readonly byte[] Program;
        private readonly byte[] Memory;
        private readonly Stack Stack;
        private OperationMode Mode;
        private int InstructionPointer;
        private bool Running;
        public Runtime(ref byte[] program, int memorySize)
        {
            Program = program;
            if (Program.Length < 4)
            {
                Logger.Error($"Invalid Program...");
                Environment.Exit((int)ExitCode.RuntimeError);   
            }
            Memory = new byte[memorySize];
            Stack = new(ref Memory);
            InstructionPointer = 4;         /* Skip the version bytes */
            Running = true;

            int progVersion = BitConverter.ToInt32(Program, 0);
            if (Version != progVersion)
            {
                Logger.Error($"Parser version is {Version} but bytecode version is {progVersion}...");
                Environment.Exit((int)ExitCode.RuntimeError);   
            }
        }

        private string BytesToFriendly(byte[] bytes)
        {
            return (Mode == OperationMode.Int ? BitConverter.ToInt32(bytes) : BitConverter.ToSingle(bytes)).ToString();
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
                Logger.Verbose($"Add | {left}+{right}");
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left + right);
                Logger.Verbose($"Add | {left}+{right}");
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
                Logger.Verbose($"Subtract | {left}-{right}");
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left - right);
                Logger.Verbose($"Subtract | {left}-{right}");
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
                Logger.Verbose($"Multiply | {left}*{right}");
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left * right);
                Logger.Verbose($"Multiply | {left}*{right}");
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
                Logger.Verbose($"Divide | {left}/{right}");
            }
            else
            {
                float right = Stack.PopFloat();
                float left = Stack.PopFloat();
                bytes = BitConverter.GetBytes(left / right);
                Logger.Verbose($"Divide | {left}/{right}");
            }
            Stack.Push(bytes);
        }

        private void PushImm()
        {
            ++InstructionPointer;
            byte[] val = GetBytesFromPrgm();
            Stack.Push(val);
            Logger.Verbose($"PushImm | Value: {BytesToFriendly(val)}");
        }
        private void PushVar()
        {
            ++InstructionPointer;
            int address = GetIntFromPrgm();
            byte[] val = Stack.ReadOffsetBytes(address);
            Stack.Push(val);
            Logger.Verbose($"PushVar | Value: {BytesToFriendly(val)} | address: {address}");
        }

        private void StoreVariable()
        {
            ++InstructionPointer;
            int address = GetIntFromPrgm();
            byte[] bytes = Stack.PopBytes();
            Stack.WriteToOffset(bytes, address);
            Logger.Verbose($"StoreVar | Value: {BytesToFriendly(bytes)} | address: {address}");
        }

        private void SetupFrame()
        {
            ++InstructionPointer;
            int size = GetIntFromPrgm();
            Stack.SetupFrame(size);
            Logger.Verbose($"SetupFrame | size: {size}");
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
            Logger.Verbose($"Move | x: {x} | y: {y}");
            // Move here
        }

        private void Delay()
        {
            float timeInSeconds = Stack.PopFloat();
            Logger.Verbose($"Delay | time: {timeInSeconds}");
            Thread.Sleep((int)(timeInSeconds * 1000));
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
    }
}