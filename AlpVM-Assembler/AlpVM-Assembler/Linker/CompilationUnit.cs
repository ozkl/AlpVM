using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AlpVM_Assembler.Linker
{
    class CompilationUnit
    {
        FileHeader mHeader = new FileHeader();

        List<SymbolUsage> mSymbolUseList = new List<SymbolUsage>();
        Dictionary<string, Symbol> mSymbols = new Dictionary<string, Symbol>();

        List<Instruction> mInstructions = new List<Instruction>();

        List<byte> mData = new List<byte>();
        List<byte> mLit = new List<byte>();

        public CompilationUnit()
        {
            UnresolvedSymbols = new List<SymbolUsage>();
        }

        public List<SymbolUsage> UnresolvedSymbols
        {
            private set;
            get;
        }

        public List<SymbolUsage> SymbolUsages
        {
            get
            {
                return mSymbolUseList;
            }
        }

        public List<Instruction> Instructions
        {
            get
            {
                return mInstructions;
            }
        }

        public List<byte> Data
        {
            get
            {
                return mData;
            }
        }

        public List<byte> Lit
        {
            get
            {
                return mLit;
            }
        }

        public FileHeader Header
        {
            get
            {
                return mHeader;
            }
        }

        public int InstructionOffset
        {
            set;
            get;
        }

        public int DataOffset
        {
            set;
            get;
        }

        public int LitOffset
        {
            set;
            get;
        }

        public Symbol GetSymbol(string symbolName)
        {
            if (mSymbols.ContainsKey(symbolName))
            {
                return mSymbols[symbolName];
            }
            return null;
        }

        public IEnumerable<Symbol> Symbols
        {
            get
            {
                return mSymbols.Values;
            }
        }

        public string File
        {
            private set;
            get;
        }

        public bool Open(string file)
        {
            mSymbols.Clear();
            mSymbolUseList.Clear();
            mInstructions.Clear();
            mData.Clear();
            mLit.Clear();
            UnresolvedSymbols.Clear();

            File = file;

            FileStream stream = new FileStream(file, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);

            mHeader.Deserialize(reader);

            if (mHeader.CodeSize > 0)
            {
                int counter = (int)mHeader.CodeSize;

                while (counter > 0)
                {
                    Int32 op = reader.ReadInt32();
                    Int16 mod1 = reader.ReadInt16();
                    Int16 mod2 = reader.ReadInt16();
                    Int32 param1 = reader.ReadInt32();
                    Int32 param2 = reader.ReadInt32();

                    OpModifier m1 = (OpModifier)mod1;
                    OpModifier m2 = (OpModifier)mod2;
                    Instruction instruction = new Instruction((OpCode)op, m1, m2, param1, param2);

                    mInstructions.Add(instruction);

                    counter -= Instruction.InstructionSize;
                }
            }

            mData = reader.ReadBytes((int)mHeader.DataSize).ToList();
            mLit = reader.ReadBytes((int)mHeader.LitSize).ToList();

            if (mHeader.SymbolDefinitionTableSize > 0)
            {
                int counter = (int)mHeader.SymbolDefinitionTableSize;

                while (counter > 0)
                {
                    Symbol symbol = Symbol.Deserialize(reader);
                    counter -= symbol.CalculateSize();
                    mSymbols.Add(symbol.Name, symbol);
                }
            }

            if (mHeader.SymbolUsageTableSize > 0)
            {
                int counter = (int)mHeader.SymbolUsageTableSize;

                while (counter > 0)
                {
                    SymbolUsage symbolUsage = SymbolUsage.Deserialize(reader);
                    counter -= symbolUsage.CalculateSize();
                    mSymbolUseList.Add(symbolUsage);
                }
            }

            return true;
        }

        public void ResolveSymbols()
        {
            foreach (var symbolUsage in mSymbolUseList)
            {
                switch (symbolUsage.Section)
                {
                    case EnSection.Code:
                        {
                            int address = -1;
                            if (mSymbols.ContainsKey(symbolUsage.Name))
                            {
                                Symbol symbol = mSymbols[symbolUsage.Name];

                                switch (symbol.Section)
                                {
                                    case EnSection.Code:
                                        address = (symbol.Address + InstructionOffset) * Instruction.InstructionSize;
                                        break;
                                    case EnSection.Data:
                                        address = DataOffset + symbol.Address + Instruction.InstructionSize * Linker.TotalInstructionCount;
                                        break;
                                }
                            }

                            if (address >= 0)
                            {
                                mInstructions[symbolUsage.Address].Parameter1 = address;
                            }
                            else
                            {
                                //throw new Exception("Symbol error. Unknown symbol: " + symbolUsage.Name);
                                UnresolvedSymbols.Add(symbolUsage);
                            }
                        }
                        break;
                    case EnSection.Data:
                        {
                            if (mSymbols.ContainsKey(symbolUsage.Name))
                            {
                                int address = mSymbols[symbolUsage.Name].Address + LitOffset + Linker.TotalDataBytes + Instruction.InstructionSize * Linker.TotalInstructionCount;
                                byte[] bytes = BitConverter.GetBytes(address);

                                mData[symbolUsage.Address + 0] = bytes[0];
                                mData[symbolUsage.Address + 1] = bytes[1];
                                mData[symbolUsage.Address + 2] = bytes[2];
                                mData[symbolUsage.Address + 3] = bytes[3];
                            }
                            else
                            {
                                //throw new Exception("Symbol error. Unknown symbol: " + symbolUsage.Name);
                                UnresolvedSymbols.Add(symbolUsage);
                            }
                        }
                        break;
                    default:
                        UnresolvedSymbols.Add(symbolUsage);
                        break;
                }
            }
        }
    }
}
