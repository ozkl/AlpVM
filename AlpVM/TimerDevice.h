#ifndef TIMERDEVICE_H
#define TIMERDEVICE_H

#include "Globals.h"

#define TIMER_COUNT 2
#define VALUE_PER_TIMER 3

class TimerDevice
{
public:
	TimerDevice(UInt32* mappedMemory);
	~TimerDevice();
	void process();
	
	inline bool isOccuredGlobal()
	{
		return mOccuredGlobal;
	}

	inline void clearOccuredGlobal()
	{
		mOccuredGlobal = false;
	}

	inline bool isTimerOccured(UInt32 id)
	{
		return mMappedMemory[VALUE_PER_TIMER * id + 0] != 0;
	}

	inline UInt32 getTimerInterval(UInt32 id)
	{
		return mMappedMemory[VALUE_PER_TIMER * id + 1];
	}

	inline UInt32 getTimerTime(UInt32 id)
	{
		return mMappedMemory[VALUE_PER_TIMER * id + 2];
	}

private:
	TimerDevice(const TimerDevice& other){}

	inline void setTimerOccured(UInt32 id, bool occured)
	{
		mMappedMemory[VALUE_PER_TIMER * id + 0] = (occured ? 1 : 0);

		if (occured)
		{
			mOccuredGlobal = true;
		}
	}

	inline void setTimerInterval(UInt32 id, UInt32 interval)
	{
		mMappedMemory[VALUE_PER_TIMER * id + 1] = interval;
	}

	inline void setTimerTime(UInt32 id, UInt32 time)
	{
		mMappedMemory[VALUE_PER_TIMER * id + 2] = time;
	}


private:
	UInt32* mMappedMemory;
	bool mOccuredGlobal;

	/*
	mMappedMemory[0] represents whether timer_0 occured
	mMappedMemory[1] represents interval of timer_0 in milliseconds
	mMappedMemory[2] represents current time of timer_0 in milliseconds

	mMappedMemory[3] represents whether timer_1 occured
	mMappedMemory[4] represents interval of timer_1 in milliseconds
	mMappedMemory[5] represents current time of timer_1 in milliseconds
	*/
};

#endif  //TIMERDEVICE_H