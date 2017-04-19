using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpVM_Assembler
{
    class Assembler
    {
        readonly string[] Seperator = { " " };
        
        Dictionary<string, Function> mFunctions = new Dictionary<string, Function>();
        List<SymbolUsage> mSymbolUseList = new List<SymbolUsage>();
        Dictionary<string, Symbol> mSymbols = new Dictionary<string, Symbol>();

        List<Instruction> mInstructions = new List<Instruction>();

        List<string> mImports = new List<string>();
        List<string> mExports = new List<string>();

        List<byte> mData = new List<byte>();
        List<byte> mLit = new List<byte>();
        int mBss = 0;

        int mCurrentArgSize = 0;

        Function mCurrentFunction;
        EnSection mCurrentSection;
        UInt32 mCurrentAlign = 1;

        public Assembler()
        {
            
        }

        OpModifier ConvertStringToOpModifierDataType(string str)
        {
            OpModifier result = OpModifier.Empty;

            if (str == "U4")
            {
                result = OpModifier.DataTypeU4;
            }
            else if (str == "P4")
            {
                result = OpModifier.DataTypeU4;
            }
            else if (str == "I4")
            {
                result = OpModifier.DataTypeI4;
            }
            else if (str == "I2")
            {
                result = OpModifier.DataTypeI2;
            }
            else if (str == "U2")
            {
                result = OpModifier.DataTypeU2;
            }
            else if (str == "I1")
            {
                result = OpModifier.DataTypeI1;
            }
            else if (str == "U1")
            {
                result = OpModifier.DataTypeU1;
            }
            else if (str == "F4")
            {
                result = OpModifier.DataTypeF4;
            }

            return result;
        }

        public int CalculateSymbolDefinitionTableSize()
        {
            int size = 0;

            foreach (var s in mSymbols)
            {
                size += s.Value.CalculateSize();
            }

            return size;
        }

        public int CalculateSymbolUsageTableSize()
        {
            int size = 0;

            foreach (var s in mSymbolUseList)
            {
                size += s.CalculateSize();
            }

            return size;
        }

        public void Assemble(string fileName)
        {
            mFunctions.Clear();
            mInstructions.Clear();
            mCurrentArgSize = 0;

            mExports.Clear();
            mImports.Clear();

            using (StreamReader reader = new StreamReader(fileName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    AssembleLine(line);
                }

                foreach (var s in mImports)
                {
                    if (mSymbols.ContainsKey(s))
                    {
                        mSymbols[s].Flag = Symbol.EnFlag.Import;
                    }
                }
                foreach (var s in mExports)
                {
                    if (mSymbols.ContainsKey(s))
                    {
                        mSymbols[s].Flag = Symbol.EnFlag.Export;
                    }
                }
            }
        }

        public void SaveBinary(string fileName)
        {
            FileStream stream = new FileStream(fileName, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(stream);

            FileHeader header = new FileHeader();
            header.MagicNumber = 5262401;//BitConverter.ToUInt32(new byte[4] {0x41, 0x4C, 0x50, 0x00}, 0);
            header.FileFormatVersion = 0;//TODO
            header.FileType = (uint)EnFileType.Raw;
            header.CodeSize = (uint)mInstructions.Count * Instruction.InstructionSize;
            header.DataSize = (uint)mData.Count;
            header.LitSize = (uint)mLit.Count;
            header.BssSize = (uint)mBss;
            header.SymbolDefinitionTableSize = (uint)CalculateSymbolDefinitionTableSize();
            header.SymbolUsageTableSize = (uint)CalculateSymbolUsageTableSize();
            header.Serialize(writer);

            foreach (var instruction in mInstructions)
            {
                writer.Write((Int32)instruction.Operation);
                writer.Write(instruction.Modifier1);
                writer.Write(instruction.Modifier2);
                writer.Write(instruction.Parameter1);
                writer.Write(instruction.Parameter2);
            }

            foreach (byte data in mData)
            {
                writer.Write(data);
            }

            foreach (byte data in mLit)
            {
                writer.Write(data);
            }

            foreach (var s in mSymbols)
            {
                s.Value.Serialize(writer);
            }

            foreach (var s in mSymbolUseList)
            {
                s.Serialize(writer);
            }

            writer.Flush();
            stream.Close();
        }

        public void SaveDissassembly(string fileName)
        {
            StreamWriter writer = new StreamWriter(fileName);
            foreach (var instruction in mInstructions)
            {
                string line = string.Format("{0}\t\t{1} {2} {3} {4}",
                    instruction.Operation.ToString(),
                    ((OpModifier)instruction.Modifier1).ToString(),
                    ((OpModifier)instruction.Modifier2).ToString(),
                    instruction.Parameter1.ToString(),
                    instruction.Parameter2.ToString()
                    );
                writer.WriteLine(line);
            }

            writer.Close();
        }

        void EmitConditionalJump(OpCode op, string label, OpModifier dataType)
        {
            mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
            mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            mInstructions.Add(new Instruction(OpCode.COMPARE, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

            mSymbolUseList.Add(new SymbolUsage(label, EnSection.Code, mInstructions.Count));

            /*
            if (mSymbolsForCode.ContainsKey(label))
            {
                int address = mSymbolsForCode[label];
                mInstructions.Add(new Instruction(op, OpModifier.Immediate | OpModifier.Far, OpModifier.Empty, address * Instruction.InstructionSize));
            }
            else*/
            {
                Instruction ins = new Instruction(op, OpModifier.Immediate | OpModifier.Absolute, OpModifier.Empty, 0);
                mInstructions.Add(ins);
                //AddUnresolvedLabel(label, ins);
            }
        }

        void AssembleLine(string line)
        {
            string[] tokens = line.Split(Seperator, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
            {
                return;
            }

            string op = tokens[0];
            string p1Str = "";
            Int32 p1 = 0;
            bool isP1Int = false;
            bool isP1Exists = (tokens.Length > 1);
            if (isP1Exists)
            {
                p1Str = tokens[1];
                isP1Int = Int32.TryParse(p1Str, out p1);
            }

            if (op.StartsWith("CNST"))
            {
                if (isP1Int)
                {
                    mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Immediate, OpModifier.Empty, p1));

                }
                else
                {
                    throw new Exception(string.Format("{0} needs parameter", op));
                }
            }
            else if (op == "ADDRLP4")
            {
                if (isP1Exists)
                {
                    int val = 0;

                    if (isP1Int)
                    {
                        val = p1;
                    }
                    else
                    {
                        if (p1Str.Contains("+"))
                        {
                            string[] values = p1Str.Split(new string[] { "+" }, StringSplitOptions.RemoveEmptyEntries);
                            if (values.Length == 2)
                            {
                                int v1 = 0;
                                int v2 = 0;

                                bool success = Int32.TryParse(values[0], out v1);
                                if (!success)
                                {
                                    throw new Exception(string.Format("{0} not understood: {1}", op, p1Str));
                                }
                                success = Int32.TryParse(values[1], out v2);
                                if (!success)
                                {
                                    throw new Exception(string.Format("{0} not understood: {1}", op, p1Str));
                                }
                                val = v1 + v2;
                            }
                            else
                            {
                                throw new Exception(string.Format("{0} not understood: {1}", op, p1Str));
                            }
                        }
                        else if (p1Str.Contains("-"))
                        {
                            string[] values = p1Str.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                            if (values.Length == 2)
                            {
                                int v1 = 0;
                                int v2 = 0;

                                bool success = Int32.TryParse(values[0], out v1);
                                if (!success)
                                {
                                    throw new Exception(string.Format("{0} not understood: {1}", op, p1Str));
                                }
                                success = Int32.TryParse(values[1], out v2);
                                if (!success)
                                {
                                    throw new Exception(string.Format("{0} not understood: {1}", op, p1Str));
                                }
                                val = v1 - v2;
                            }
                            else
                            {
                                throw new Exception(string.Format("{0} not understood: {1}", op, p1Str));
                            }
                        }
                    }
                    mInstructions.Add(new Instruction(OpCode.ASSIGN, OpModifier.Register, OpModifier.Register, (int)Register.Alp1, (int)Register.BasePointer));
                    mInstructions.Add(new Instruction(OpCode.ADD, OpModifier.Register, OpModifier.Immediate, (int)Register.Alp1, val + 4 + mCurrentFunction.ArgumentsCallDataSize));
                    mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                }
                else
                {
                    throw new Exception(string.Format("{0} needs parameter", op));
                }
            }
            else if (op == "ADDRFP4")
            {
                if (isP1Int)
                {
                    mInstructions.Add(new Instruction(OpCode.ASSIGN, OpModifier.Register, OpModifier.MemoryAtRegister, (int)Register.Alp1, (int)Register.BasePointer));//Going to previous stack frame, because LCC bytecode assumes args are there
                    mInstructions.Add(new Instruction(OpCode.ADD, OpModifier.Register, OpModifier.Immediate, (int)Register.Alp1, p1 + 4));//0 is previous of previous function's ebp value, so args start from +4
                    mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                }
                else
                {
                    throw new Exception(string.Format("{0} needs parameter", op));
                }
            }
            else if (op == "ADDRGP4")
            {
                if (isP1Exists)
                {
                    if (p1Str.Contains("+"))
                    {
                        string[] values = p1Str.Split(new string[] { "+" }, StringSplitOptions.RemoveEmptyEntries);
                        if (values.Length == 2)
                        {
                            string symbol = values[0];
                            int v2 = 0;

                            bool success = Int32.TryParse(values[1], out v2);
                            if (!success)
                            {
                                throw new Exception(string.Format("{0} not understood: {1}", op, p1Str));
                            }

                            mSymbolUseList.Add(new SymbolUsage(symbol, EnSection.Code, mInstructions.Count));
                            mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Immediate, OpModifier.Empty, 0));

                            mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp5));
                            mInstructions.Add(new Instruction(OpCode.ASSIGN, OpModifier.Register, OpModifier.Immediate, (int)Register.Alp6, v2));
                            mInstructions.Add(new Instruction(OpCode.ADD, OpModifier.Register, OpModifier.Register, (int)Register.Alp5, (int)Register.Alp6));
                            mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp5));
                        }
                        else
                        {
                            throw new Exception(string.Format("{0} not understood: {1}", op, p1Str));
                        }
                    }
                    else
                    {
                        string label = p1Str;

                        mSymbolUseList.Add(new SymbolUsage(label, EnSection.Code, mInstructions.Count));

                        Instruction ins = new Instruction(OpCode.PUSH, OpModifier.Immediate, OpModifier.Empty, 0);
                        mInstructions.Add(ins);
                    }
                }
                else
                {
                    throw new Exception(string.Format("{0} needs label parameter", op));
                }
            }
            else if (op.StartsWith("ARG"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
                //mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                mInstructions.Add(new Instruction(OpCode.ASSIGN, OpModifier.Memory | OpModifier.BpOffset, OpModifier.Register, mCurrentArgSize + 4, (int)Register.Alp1));

                mCurrentArgSize += 4;
            }
            else if (op == "ASGNB")
            {
                if (isP1Int)
                {
                    mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
                    mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                    mInstructions.Add(new Instruction(OpCode.SET_COUNTER, OpModifier.Immediate, OpModifier.Empty, p1));
                    mInstructions.Add(new Instruction(OpCode.COPY_MEMORY_ARRAY, OpModifier.Register, OpModifier.Register, (int)Register.Alp2, (int)Register.Alp1));
                }
                else
                {
                    throw new Exception(string.Format("{0} needs parameter", op));
                }
            }
            else if (op.StartsWith("ASGN"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(4));

                mInstructions.Add(new Instruction(OpCode.ASSIGN, OpModifier.MemoryAtRegister | dataType, OpModifier.Register, (int)Register.Alp2, (int)Register.Alp1));
            }
            else if (op == "INDIRB")
            {
                //Do nothing. We have address already on top of stack.
            }
            else if (op.StartsWith("INDIR"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
                mInstructions.Add(new Instruction(OpCode.ASSIGN, OpModifier.Register, OpModifier.MemoryAtRegister, (int)Register.Alp2, (int)Register.Alp1));
                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
            }
            else if (op.StartsWith("LABEL"))
            {
                if (isP1Exists)
                {
                    if (mSymbols.ContainsKey(p1Str))
                    {
                        throw new Exception(string.Format("LABEL {0} already defined", p1Str));
                    }

                    if (mCurrentSection == EnSection.Code)
                    {
                        //mSymbolsForCode.Add(p1Str, mInstructions.Count);
                        mSymbols.Add(p1Str, new Symbol(p1Str, EnSection.Code, mInstructions.Count));
                    }
                    else if (mCurrentSection == EnSection.Data)
                    {
                        int dataIndex = mData.Count;
                        if (mCurrentAlign > 1)
                        {
                            int mod = dataIndex % (int)mCurrentAlign;
                            if (mod != 0)
                            {
                                //Align to mCurrentAlign byte boundry
                                int bytesNeeded = (int)mCurrentAlign - mod;
                                for (int i = 0; i < bytesNeeded; ++i)
                                {
                                    mData.Add(0);//Fill bytesNeeded bytes
                                }
                                dataIndex = mData.Count;
                            }
                        }
                        //mSymbolsForData.Add(p1Str, dataIndex);
                        mSymbols.Add(p1Str, new Symbol(p1Str, EnSection.Data, dataIndex));
                    }
                    else if (mCurrentSection == EnSection.Lit)
                    {
                        int dataIndex = mLit.Count;
                        if (mCurrentAlign > 1)
                        {
                            int mod = dataIndex % (int)mCurrentAlign;
                            if (mod != 0)
                            {
                                //Align to mCurrentAlign byte boundry
                                int bytesNeeded = (int)mCurrentAlign - mod;
                                for (int i = 0; i < bytesNeeded; ++i)
                                {
                                    mLit.Add(0);//Fill bytesNeeded bytes
                                }
                                dataIndex = mLit.Count;
                            }
                        }
                        //mSymbolsForLit.Add(p1Str, dataIndex);
                        mSymbols.Add(p1Str, new Symbol(p1Str, EnSection.Lit, dataIndex));
                    }
                    else if (mCurrentSection == EnSection.Bss)
                    {
                        int dataIndex = mBss;
                        if (mCurrentAlign > 1)
                        {
                            int mod = dataIndex % (int)mCurrentAlign;
                            if (mod != 0)
                            {
                                //Align to mCurrentAlign byte boundry
                                int bytesNeeded = (int)mCurrentAlign - mod;
                                mBss += bytesNeeded;
                                dataIndex = mBss;
                            }
                        }
                        //mSymbolsForBss.Add(p1Str, dataIndex);
                        mSymbols.Add(p1Str, new Symbol(p1Str, EnSection.Bss, dataIndex));
                    }
                }
                else
                {
                    throw new Exception(string.Format("LABEL needs a parameter"));
                }
            }
            else if (op.StartsWith("ADD"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.ADD, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("SUB"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.SUB, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("MUL"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.MUL, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("DIV"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.DIV, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("MOD"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.MOD, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("LSH"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.SHIFTLEFT, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("RSH"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.SHIFTRIGHT, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("BAND"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.BITWISE_AND, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("BCOM"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.BITWISE_COMPLEMENT, OpModifier.Register | dataType, OpModifier.Empty, (int)Register.Alp1));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("BOR"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.BITWISE_OR, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("BXOR"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.BITWISE_XOR, OpModifier.Register | dataType, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("CVF"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.CONVERT, OpModifier.Register | OpModifier.DataTypeF4, OpModifier.Register | dataType, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("CVI"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.CONVERT, OpModifier.Register | OpModifier.DataTypeI4, OpModifier.Register | dataType, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("CVU"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.CONVERT, OpModifier.Register | OpModifier.DataTypeU4, OpModifier.Register | dataType, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("CVP"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));

                OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                mInstructions.Add(new Instruction(OpCode.CONVERT, OpModifier.Register | OpModifier.DataTypeU4, OpModifier.Register | dataType, (int)Register.Alp1, (int)Register.Alp2));

                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("JUMP"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));//Address for JUMP is in Alp1

                mInstructions.Add(new Instruction(OpCode.JUMP, OpModifier.Register | OpModifier.Absolute, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op.StartsWith("EQ"))
            {
                if (isP1Exists)
                {
                    string label = p1Str;

                    OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                    EmitConditionalJump(OpCode.JUMPIF_EQ, label, dataType);
                }
                else
                {
                    throw new Exception(string.Format("{0} needs label parameter", op));
                }
            }
            else if (op.StartsWith("NE"))
            {
                if (isP1Exists)
                {
                    string label = p1Str;

                    OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                    EmitConditionalJump(OpCode.JUMPIF_NEQ, label, dataType);
                }
                else
                {
                    throw new Exception(string.Format("{0} needs label parameter", op));
                }
            }
            else if (op.StartsWith("LE"))
            {
                if (isP1Exists)
                {
                    string label = p1Str;

                    OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                    EmitConditionalJump(OpCode.JUMPIF_LTE, label, dataType);
                }
                else
                {
                    throw new Exception(string.Format("{0} needs label parameter", op));
                }
            }
            else if (op.StartsWith("LT"))
            {
                if (isP1Exists)
                {
                    string label = p1Str;

                    OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                    EmitConditionalJump(OpCode.JUMPIF_LT, label, dataType);
                }
                else
                {
                    throw new Exception(string.Format("{0} needs label parameter", op));
                }
            }
            else if (op.StartsWith("GE"))
            {
                if (isP1Exists)
                {
                    string label = p1Str;

                    OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                    EmitConditionalJump(OpCode.JUMPIF_GTE, label, dataType);
                }
                else
                {
                    throw new Exception(string.Format("{0} needs label parameter", op));
                }
            }
            else if (op.StartsWith("GT"))
            {
                if (isP1Exists)
                {
                    string label = p1Str;

                    OpModifier dataType = ConvertStringToOpModifierDataType(op.Substring(op.Length - 2));
                    EmitConditionalJump(OpCode.JUMPIF_GT, label, dataType);
                }
                else
                {
                    throw new Exception(string.Format("{0} needs label parameter", op));
                }
            }
            else if (op.StartsWith("CALL"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));//Address for CALL is in Alp1

                /*
                if (mCurrentArgSize > 0)
                {
                    mInstructions.Add(new Instruction(OpCode.REVERSE_STACK, OpModifier.Immediate, OpModifier.Immediate, 0, mCurrentArgSize / 4));
                }*/

                mInstructions.Add(new Instruction(OpCode.CALL, OpModifier.Register | OpModifier.Absolute, OpModifier.Empty, (int)Register.Alp1));

                //mInstructions.Add(new Instruction(OpCode.DEC_SP, OpModifier.Immediate, OpModifier.Empty, mCurrentArgSize));//Cleanup

                mCurrentArgSize = 0;

                //We assume, we have put return value to Return register. So we should push it here.
                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (Int32)Register.Return));
            }
            else if (op.StartsWith("RET"))
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Return));
            }
            else if (op.StartsWith("proc"))
            {
                if (tokens.Length > 3)
                {
                    string functionName = p1Str;
                    Int32 localSize = Int32.Parse(tokens[2]);
                    Int32 argSize = Int32.Parse(tokens[3]);
                    
                    if (mFunctions.ContainsKey(functionName))
                    {
                        throw new Exception(string.Format("Function {0} already exists", functionName));
                    }
                    else
                    {
                        if (null != mCurrentFunction)
                        {
                            throw new Exception(string.Format("Already inside a function, found a new function: {0}", functionName));
                        }
                        mCurrentFunction = new Function(functionName, mInstructions.Count, localSize, argSize);
                        mFunctions.Add(functionName, mCurrentFunction);

                        if (mSymbols.ContainsKey(functionName))
                        {
                            throw new Exception(string.Format("Symbol already defined: {0}", functionName));
                        }
                        else
                        {
                            mSymbols.Add(functionName, new Symbol(functionName, EnSection.Code, mInstructions.Count));
                        }
                    }

                    //Prolog
                    mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (Int32)Register.BasePointer));
                    mInstructions.Add(new Instruction(OpCode.ASSIGN, OpModifier.Register, OpModifier.Register, (Int32)Register.BasePointer, (Int32)Register.StackPointer));
                    mInstructions.Add(new Instruction(OpCode.INC_SP, OpModifier.Immediate, OpModifier.Empty, localSize + argSize));

                    //TODO:Save registers here (excluding BS, SP)
                }
                else
                {
                    throw new Exception(string.Format("{0} needs two parameters", op));
                }
            }
            else if (op.StartsWith("endproc"))
            {
                if (tokens.Length > 3)
                {
                    string functionName = p1Str;
                    Int32 localSize = Int32.Parse(tokens[2]);
                    Int32 argSize = Int32.Parse(tokens[3]);

                    if (null == mCurrentFunction)
                    {
                        throw new Exception(string.Format("Not in a function, found end of function", functionName));
                    }

                    mCurrentFunction = null;

                    //TODO:Restore registers here (excluding BS, SP)

                    //Epilog
                    mInstructions.Add(new Instruction(OpCode.ASSIGN, OpModifier.Register, OpModifier.Register, (Int32)Register.StackPointer, (Int32)Register.BasePointer));
                    mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (Int32)Register.BasePointer));

                    /*
                    if (IsInterruptHandler)
                    {
                        AddInstruction(EnOpCode.OC_IRET);
                    }
                    else
                    {
                        AddInstruction(EnOpCode.OC_RET);
                    }*/
                    mInstructions.Add(new Instruction(OpCode.RET));
                }
                else
                {
                    throw new Exception(string.Format("{0} needs two parameters", op));
                }
            }
            else if (op == "export")
            {
                if (!mExports.Contains(p1Str))
                {
                    mExports.Add(p1Str);
                }
            }
            else if (op == "code")
            {
                mCurrentSection = EnSection.Code;
            }
            else if (op == "data")
            {
                mCurrentSection = EnSection.Data;
            }
            else if (op == "bss")
            {
                mCurrentSection = EnSection.Bss;
            }
            else if (op == "lit")
            {
                mCurrentSection = EnSection.Lit;
            }
            else if (op == "align")
            {
                if (isP1Int)
                {
                    mCurrentAlign = (UInt32)p1;
                }
                else
                {
                    throw new Exception(string.Format("{0} needs parameter", op));
                }
            }
            else if (op == "skip")
            {
                if (isP1Int)
                {
                    if (mCurrentSection != EnSection.Bss)
                    {
                        throw new Exception("Encountered a skip in non-bss section");
                    }
                    else
                    {
                        mBss += p1;
                    }
                }
                else
                {
                    throw new Exception(string.Format("{0} needs parameter", op));
                }
            }
            else if (op == "byte")
            {
                if (isP1Int)
                {
                    Int32 p2 = 0;
                    bool isP2Int = false;
                    bool isP2Exists = (tokens.Length > 2);
                    if (isP2Exists)
                    {
                        string p2Str = tokens[2];
                        isP2Int = Int32.TryParse(p2Str, out p2);
                    }

                    if (isP2Int)
                    {
                        List<byte> target = null;
                        if (mCurrentSection == EnSection.Data)
                        {
                            target = mData;
                        }
                        else if (mCurrentSection == EnSection.Lit)
                        {
                            target = mLit;
                        }
                        else
                        {
                            throw new Exception("Unknown section while parsing byte");
                        }

                        if (null != target)
                        {
                            switch (p1)
                            {
                                case 1:
                                    target.Add((byte)(p2 & 0x000000FF));
                                    break;
                                case 2:
                                    {
                                        target.Add((byte)(p2 & 0x000000FF));
                                        target.Add((byte)((p2 & 0x0000FF00) >> 8));
                                    }
                                    break;
                                case 4:
                                    {
                                        target.Add((byte)(p2 & 0x000000FF));
                                        target.Add((byte)((p2 & 0x0000FF00) >> 8));
                                        target.Add((byte)((p2 & 0x00FF0000) >> 16));
                                        target.Add((byte)((p2 & 0xFF000000) >> 24));
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("{0} needs two integer parameters", op));
                    }
                }
                else
                {
                    throw new Exception(string.Format("{0} needs two integer parameters", op));
                }
            }
            else if (op == "address")
            {
                if (isP1Exists)
                {
                    string label = p1Str;

                    if (mCurrentSection != EnSection.Data)
                    {
                        throw new Exception("Encountered 'address' in non-data section");
                    }
                    else
                    {
                        mSymbolUseList.Add(new SymbolUsage(label, EnSection.Data, mData.Count));

                        //These 4 bytes will be filled with address of the label when linking
                        mData.Add(0);
                        mData.Add(0);
                        mData.Add(0);
                        mData.Add(0);
                    }
                }
                else
                {
                    throw new Exception(string.Format("{0} needs parameter", op));
                }
            }
            else if (op == "import")
            {
                if (!mImports.Contains(p1Str))
                {
                    mImports.Add(p1Str);
                }
            }
            else if (op == "_SETCOUNTER")
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
                mInstructions.Add(new Instruction(OpCode.SET_COUNTER, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op == "_READCOUNTER")
            {
                mInstructions.Add(new Instruction(OpCode.READ_COUNTER, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
                mInstructions.Add(new Instruction(OpCode.PUSH, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
            }
            else if (op == "_MEMSET")
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp2));
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Register, OpModifier.Empty, (int)Register.Alp1));
                mInstructions.Add(new Instruction(OpCode.SET_MEMORY_ARRAY, OpModifier.Register, OpModifier.Register, (int)Register.Alp1, (int)Register.Alp2));
            }
            else if (op == "_POP")
            {
                mInstructions.Add(new Instruction(OpCode.POP, OpModifier.Empty, OpModifier.Empty));
            }
            else
            {
                throw new Exception(string.Format("{0} not implemented", op));
            }
        }
    }
}
