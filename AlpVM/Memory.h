#ifndef MEMORY_H
#define MEMORY_H

#include "Globals.h"

class Memory
{
public:
	Memory(UInt32 memorySize);
	~Memory();
	void setByte(UInt32 address, UByte byte);
	UByte getByte(UInt32 address) const;
	UByte* getRawData();
	void clear();

	template<typename T>
	UInt32 setData(UInt32 address, const T& data)
	{
		UInt32 size = sizeof(T);

		const UByte* pointer = (const UByte*)&data;

		UInt32 i = 0;
		for (i = 0; i < size; ++i)
		{
			mData[address + i] = (pointer[i]);
		}

		return address + i;
	}

	template<typename T>
	UInt32 getData(UInt32 address, T& data)
	{
		UInt32 size = sizeof(T);

		UByte* pointer = (UByte*)&data;

		UInt32 i = 0;
		for (i = 0; i < size; ++i)
		{
			pointer[i] = mData[address + i];
		}

		return address + i;
	}

private:
	UByte* mData;
	UInt32 mMemorySize;
};

#endif //MEMORY_H