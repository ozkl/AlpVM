#include "KeyboardDevice.h"


KeyboardDevice::KeyboardDevice(UInt32* mappedMemory)
	:
	mMappedMemory(mappedMemory)
{
}


KeyboardDevice::~KeyboardDevice()
{
}


void KeyboardDevice::setKey(UInt32 keyCode)
{
	mMappedMemory[0] = keyCode;
}

UInt32 KeyboardDevice::getKey() const
{
	return mMappedMemory[0];
}