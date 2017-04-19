#ifndef SYSTEMTIME_H
#define SYSTEMTIME_H

#include <ctime>

#include "Globals.h"

class SystemTime
{
public:
	SystemTime(UInt32* mappedMemory);
	~SystemTime();

	inline void process()
	{
		*mMappedMemory = (UInt32)time(NULL);
	}

private:
	UInt32* mMappedMemory;
};

#endif //SYSTEMTIME_H