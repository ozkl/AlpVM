#include "SystemTime.h"


SystemTime::SystemTime(UInt32* mappedMemory)
		:
	mMappedMemory(mappedMemory)
{
	*mMappedMemory = 0;
}


SystemTime::~SystemTime()
{
}
