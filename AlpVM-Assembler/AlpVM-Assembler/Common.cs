using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AlpVM_Assembler
{
    public enum EnSection
    {
        Data,
        Bss,
        Lit,
        Code
    }

    public enum EnFileType
    {
        Raw,
        Executable
    }

    public class FileHeader
    {
        public UInt32 MagicNumber;
        public UInt32 FileFormatVersion;
        public UInt32 FileType;
        public UInt32 CodeSize;
        public UInt32 DataSize;
        public UInt32 LitSize;
        public UInt32 BssSize;
        public UInt32 SymbolDefinitionTableSize;
        public UInt32 SymbolUsageTableSize;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(MagicNumber);
            writer.Write(FileFormatVersion);
            writer.Write(FileType);
            writer.Write(CodeSize);
            writer.Write(DataSize);
            writer.Write(LitSize);
            writer.Write(BssSize);
            writer.Write(SymbolDefinitionTableSize);
            writer.Write(SymbolUsageTableSize);
        }

        public void Deserialize(BinaryReader reader)
        {
            MagicNumber = reader.ReadUInt32();
            FileFormatVersion = reader.ReadUInt32();
            FileType = reader.ReadUInt32();
            CodeSize = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            LitSize = reader.ReadUInt32();
            BssSize = reader.ReadUInt32();
            SymbolDefinitionTableSize = reader.ReadUInt32();
            SymbolUsageTableSize = reader.ReadUInt32();
        }
    }

    public class SymbolUsage
    {
        public SymbolUsage(string name, EnSection section, int address)
        {
            Name = name;
            Section = section;
            Address = address;
        }

        private SymbolUsage()
        {
        }

        public string Name;
        public EnSection Section;
        public int Address;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Name.Length);
            writer.Write(Encoding.ASCII.GetBytes(Name));
            writer.Write((byte)Section);
            writer.Write(Address);
        }

        public static SymbolUsage Deserialize(BinaryReader reader)
        {
            SymbolUsage obj = new SymbolUsage();
            int strLength = reader.ReadInt32();
            byte[] nameBytes = reader.ReadBytes(strLength);
            obj.Name = Encoding.ASCII.GetString(nameBytes);
            obj.Section = (EnSection)reader.ReadByte();
            obj.Address = reader.ReadInt32();

            return obj;
        }

        public int CalculateSize()
        {
            int size = 0;

            size += 4;//name length
            size += Name.Length;
            size += 1; //section
            size += 4; //Address

            return size;
        }
    }

    public class Symbol
    {
        public enum EnFlag
        {
            None,
            Export,
            Import
        }

        public Symbol(string name, EnSection section, int address, EnFlag flag = EnFlag.None)
        {
            Name = name;
            Section = section;
            Address = address;
            Flag = flag;
        }

        private Symbol()
        {
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Name.Length);
            writer.Write(Encoding.ASCII.GetBytes(Name));
            writer.Write((byte)Section);
            writer.Write(Address);
            writer.Write((byte)Flag);
        }

        public static Symbol Deserialize(BinaryReader reader)
        {
            Symbol symbol = new Symbol();

            int strLength = reader.ReadInt32();
            byte[] nameBytes = reader.ReadBytes(strLength);
            symbol.Name = Encoding.ASCII.GetString(nameBytes);
            symbol.Section = (EnSection)reader.ReadByte();
            symbol.Address = reader.ReadInt32();
            symbol.Flag = (EnFlag)reader.ReadByte();

            return symbol;
        }

        public int CalculateSize()
        {
            int size = 0;

            size += 4;//name length
            size += Name.Length;
            size += 1; //section
            size += 4; //Address
            size += 1; //Flag

            return size;
        }

        public string Name;
        public EnSection Section;
        public int Address;
        public EnFlag Flag;
    }
}
