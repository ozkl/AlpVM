#include "SymbolUsage.h"


SymbolUsage::SymbolUsage()
	:
NameLength(0),
	Section(Data),
	Address(0)
{
}


SymbolUsage::~SymbolUsage()
{
}

SymbolUsage SymbolUsage::deserialize(std::ifstream& inFile)
{
	SymbolUsage result;

	int nameLength = 0;
	char section = 0;
	int address = 0;

	inFile.read((char*)&nameLength, 4);
	inFile.seekg(nameLength, std::ios_base::cur);
	inFile.read((char*)&section, 1);
	inFile.read((char*)&address, 4);

	result.NameLength = nameLength;
	result.Section = (EnSection)section;
	result.Address = address;

	return result;
}

int SymbolUsage::calculateSize()
{
    int size = 0;

    size += 4;//name length
    size += NameLength;
    size += 1; //section
    size += 4; //Address

    return size;
}