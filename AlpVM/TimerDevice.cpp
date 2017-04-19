#include <ctime>

#include "TimerDevice.h"


TimerDevice::TimerDevice(UInt32* mappedMemory)
	:
	mMappedMemory(mappedMemory),
	mOccuredGlobal(false)
{
	for (UInt32 i = 0; i < TIMER_COUNT; ++i)
	{
		setTimerOccured(i, false);
		setTimerInterval(i, 0);
		setTimerTime(i, 0);
	}
}


TimerDevice::~TimerDevice()
{
}

void TimerDevice::process()
{
	UInt32 interval = 0;
	UInt32 time = 0;

	UInt32 now = (UInt32)((clock() / (float)CLOCKS_PER_SEC) * 1000);

	for (UInt32 i = 0; i < TIMER_COUNT; ++i)
	{
		interval = getTimerInterval(i);
		if (interval > 0)
		{
			time = getTimerTime(i);
			if (now - time >= interval)
			{
				setTimerOccured(i, true);
				setTimerTime(i, now);
			}
		}
	}
}