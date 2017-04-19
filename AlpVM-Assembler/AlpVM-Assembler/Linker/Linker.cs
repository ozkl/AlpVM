using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AlpVM_Assembler.Linker
{
    class Linker
    {
        List<string> mInputFiles = new List<string>();

        List<CompilationUnit> mUnits = new List<CompilationUnit>();

        List<Instruction> mInstructions = new List<Instruction>();

        List<byte> mData = new List<byte>();
        List<byte> mLit = new List<byte>();
        int mBss;

        Dictionary<string, KeyValuePair<Symbol, CompilationUnit>> mAllSymbols = new Dictionary<string, KeyValuePair<Symbol, CompilationUnit>>();

        List<SymbolUsage> mSymbolUsages = new List<SymbolUsage>();

        public static int TotalInstructionCount
        {
            private set;
            get;
        }

        public static int TotalDataBytes
        {
            private set;
            get;
        }

        public static int TotalLitBytes
        {
            private set;
            get;
        }

        public void AddInputFile(string file)
        {
            mInputFiles.Add(file);
        }

        public void Link()
        {
            mUnits.Clear();
            mInstructions.Clear();
            mData.Clear();
            mLit.Clear();
            mBss = 0;
            mAllSymbols.Clear();
            mSymbolUsages.Clear();

            //Placeholder for calling main
            //mInstructions.Add(new Instruction(OpCode.NOOPERATION));
            //mInstructions.Add(new Instruction(OpCode.HALT));//After main returns, HALT

            foreach (string file in mInputFiles)
            {
                CompilationUnit unit = new CompilationUnit();
                bool success = unit.Open(file);
                if (success)
                {
                    unit.InstructionOffset = mInstructions.Count;
                    unit.DataOffset = mData.Count;
                    unit.LitOffset = mLit.Count;

                    foreach (var i in unit.Instructions)
                    {
                        mInstructions.Add(i);
                    }

                    foreach (var b in unit.Data)
                    {
                        mData.Add(b);
                    }

                    foreach (var b in unit.Lit)
                    {
                        mLit.Add(b);
                    }
                    
                    mBss += (int)unit.Header.BssSize;

                    mUnits.Add(unit);
                }
            }

            TotalInstructionCount = mInstructions.Count;
            TotalDataBytes = mData.Count;
            TotalLitBytes = mLit.Count;

            //Resolve internal symbols (after this, data and instructions will be modified in the unit)
            foreach (var unit in mUnits)
            {
                unit.ResolveSymbols();
            }

            mInstructions.Clear();
            mData.Clear();
            mLit.Clear();
            foreach (var unit in mUnits)
            {
                foreach (var i in unit.Instructions)
                {
                    mInstructions.Add(i);
                }

                foreach (var b in unit.Data)
                {
                    mData.Add(b);
                }

                foreach (var b in unit.Lit)
                {
                    mLit.Add(b);
                }
            }
            
            foreach (var unit in mUnits)
            {
                foreach (var s in unit.Symbols)
                {
                    if (s.Flag == Symbol.EnFlag.Export)
                    {
                        if (mAllSymbols.ContainsKey(s.Name))
                        {
                            throw new Exception(string.Format("Symbol '{0}' in {1} is already defined in {2}", s.Name, unit.File, mAllSymbols[s.Name].Value.File));
                        }
                        else
                        {
                            mAllSymbols.Add(s.Name, new KeyValuePair<Symbol, CompilationUnit>(s, unit));
                        }
                    }
                }
            }

            ResolveExternals();

            //save data and lit symbol usage addresses (including internals) here in order for machine to adjust addresses in runtime
            int symbolId = 0;
            foreach (var unit in mUnits)
            {
                foreach (SymbolUsage su in unit.SymbolUsages)
                {
                    //At this stage, symbol name is not important

                    int address = su.Address;
                    switch (su.Section)
                    {
                        case EnSection.Code:
                            address += unit.InstructionOffset;
                            break;
                        case EnSection.Data:
                            address += unit.DataOffset;
                            break;
                    }
                    SymbolUsage newSu = new SymbolUsage((++symbolId).ToString(), su.Section, address);
                    mSymbolUsages.Add(newSu);
                    
                }
            }
            /*
            Symbol symbolMain = null;
            foreach (var unit in mUnits)
            {
                symbolMain = unit.GetSymbol("main");
                if (null != symbolMain)
                {
                    if (symbolMain.Section == EnSection.Code)
                    {
                        mInstructions[0].Operation = OpCode.CALL;
                        mInstructions[0].Modifier1 = (UInt16)(OpModifier.Immediate | OpModifier.Absolute);
                        mInstructions[0].Parameter1 = (symbolMain.Address + unit.InstructionOffset) * Instruction.InstructionSize;
                    }
                    else
                    {
                        throw new Exception("main function not found!");
                    }
                    break;
                }
            }
            if (null == symbolMain)
            {
                throw new Exception("main() could not be found!");
            }*/
        }

        //May have errors
        void ResolveExternals()
        {
            foreach (var unit in mUnits)
            {
                foreach (var symbolUsage in unit.UnresolvedSymbols)
                {
                    if (mAllSymbols.ContainsKey(symbolUsage.Name))
                    {
                        var symbolAndUnit = mAllSymbols[symbolUsage.Name];
                        Symbol symbol = symbolAndUnit.Key;
                        CompilationUnit symbolOwnerUnit = symbolAndUnit.Value;

                        switch (symbolUsage.Section)
                        {
                            case EnSection.Code:
                                {
                                    int address = -1;

                                    switch (symbol.Section)
                                    {
                                        case EnSection.Code:
                                            address = (symbol.Address + symbolOwnerUnit.InstructionOffset) * Instruction.InstructionSize;
                                            break;
                                        case EnSection.Data:
                                            address = symbolOwnerUnit.DataOffset + symbol.Address + Instruction.InstructionSize * Linker.TotalInstructionCount;
                                            break;
                                    }

                                    if (address >= 0)
                                    {
                                        mInstructions[unit.InstructionOffset + symbolUsage.Address].Parameter1 = address;
                                    }
                                    else
                                    {
                                        throw new Exception(string.Format("Symbol location not found: {0}", symbol.Name));
                                    }
                                }
                                break;
                            case EnSection.Data:
                                {
                                    int address = symbol.Address + symbolOwnerUnit.LitOffset + Linker.TotalDataBytes + Instruction.InstructionSize * Linker.TotalInstructionCount;
                                    byte[] bytes = BitConverter.GetBytes(address);

                                    mData[unit.DataOffset + symbolUsage.Address + 0] = bytes[0];
                                    mData[unit.DataOffset + symbolUsage.Address + 1] = bytes[1];
                                    mData[unit.DataOffset + symbolUsage.Address + 2] = bytes[2];
                                    mData[unit.DataOffset + symbolUsage.Address + 3] = bytes[3];
                                }
                                break;
                            default:
                                throw new Exception(string.Format("Symbol section not found: {0}", symbol.Name));
                                break;
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("Unresolved symbol '{0}' used in {1}", symbolUsage.Name, unit.File));
                    }
                }
            }
        }

        public int CalculateSymbolUsageTableSize()
        {
            int size = 0;

            foreach (var s in mSymbolUsages)
            {
                size += s.CalculateSize();
            }

            return size;
        }

        public void SaveBinary(string fileName)
        {
            FileStream stream = new FileStream(fileName, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(stream);

            FileHeader header = new FileHeader();
            header.MagicNumber = 5262401;
            header.FileFormatVersion = 0;//TODO
            header.FileType = (uint)EnFileType.Executable;
            header.CodeSize = (uint)mInstructions.Count * Instruction.InstructionSize;
            header.DataSize = (uint)mData.Count;
            header.LitSize = (uint)mLit.Count;
            header.BssSize = (uint)mBss;
            header.SymbolDefinitionTableSize = 0;
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

            foreach (var s in mSymbolUsages)
            {
                s.Serialize(writer);
            }

            writer.Flush();
            stream.Close();
        }
    }
}
