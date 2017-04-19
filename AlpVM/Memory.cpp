#include <cstring>

#include "Memory.h"


Memory::Memory(UInt32 memorySize)
{
	mMemorySize = memorySize;

	mData = new UByte[mMemorySize];

	clear();
}


Memory::~Memory()
{
	delete [] mData;
}


void Memory::setByte(UInt32 address, UByte byte)
{
	mData[address] = byte;
}

UByte Memory::getByte(UInt32 address) const
{
	return mData[address];
}

UByte* Memory::getRawData()
{
	return mData;
}

void Memory::clear()
{
	memset(mData, 0, mMemorySize);
}