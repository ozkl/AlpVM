#ifndef KEYBOARDDEVICE_H
#define KEYBOARDDEVICE_H

#include "Globals.h"

class KeyboardDevice
{
public:
	KeyboardDevice(UInt32* mappedMemory);
	~KeyboardDevice();
	void setKey(UInt32 keyCode);
	UInt32 getKey() const;

private:
	UInt32* mMappedMemory;
	//mMappedMemory[0] is the key pressed
};

#endif //KEYBOARDDEVICE_H