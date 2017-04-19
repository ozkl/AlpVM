#ifndef VIRTUALMACHINE_H
#define VIRTUALMACHINE_H

#include <vector>

#include "Globals.h"

#include "FileHeader.h"

#include "SymbolUsage.h"

#define STACK_REVERSE_BUFFER_SIZE 256

class Memory;
class SystemTime;
class TimerDevice;
class KeyboardDevice;
struct Instruction;

class VirtualMachine
{
public:
	VirtualMachine();
	~VirtualMachine();
	MachineState* getMachineState();
	void restart();
	UInt32 executeInstruction();
	bool openFromFile(const char* file);
	void clearMemory();
	UByte* getMemory();
	UByte* getVideoMemory();
	void interrupt(Interrupt i);
	void sendKey(UInt32 keyCode);

	inline UInt32 getExecutionStartLocation()
	{
		return mExecutionPointerStart;
	}

	inline UInt32 getExecutionCurrentLocation()
	{
		return mMachineState.mExecutionPointer;
	}

	inline bool isHalted() const
	{
		return mMachineState.mHalted;
	}

	inline bool isPaused() const
	{
		return mMachineState.mPaused;
	}

	//This pause/resume mechanism is only intended for
	//using from outside. Actually this does not pause the machine.
	//Machine is executed from outside by executeInstruction().
	//Caller is supposed to check isPaused() and decide to call executeInstruction().
	inline void pause()
	{
		mMachineState.mPaused = true;
	}

	inline void resume()
	{
		mMachineState.mPaused = false;
	}

	void setRegisterValue(UInt32 index, UInt32 value);
	UInt32 getRegisterValue(UInt32 index) const;

private:
	void relocateSymbols();

	void pushReg(UInt32 index);
	void pushImmediate(Int32 value);
	void popReg(UInt32 index);
	Int32 pop();
	void push(Int32 data);
	void pushPart();
	void popPart();
	void pushAll();
	void popAll();
	Int32 readValue(UInt32 imOrRegOrMem, UInt16 flags);
	void writeValue(UInt32 regOrMem, UInt16 flags, Int32 data);
	Int32 readMemValue(UInt32 addressOrRegister, UInt16 flags);
	void writeMemValue(UInt32 addressOrRegister, UInt16 flags, Int32 data);
	bool isJumpAddressValid(UInt32 address);
	UInt32 getJumpAddress(UInt32 imOrRegOrMem, UInt16 flags);
	UInt32 jump(UInt32 address);
	UInt32 jumpSmart(UInt32 imOrRegOrMem, UInt16 flags);
	void compare(Instruction& ins);
	void doArithmatic(Instruction& ins);
	void doComplement(Instruction& ins);
	void doNegative(Instruction& ins);
	void doConvert(Instruction& ins);

private:
	Memory* mMemory;
	SystemTime* mSystemTime;
	TimerDevice* mTimerDevice;
	KeyboardDevice* mKeyboardDevice;
	UInt32 mLoadedProgramInstructionCount;
	UInt32 mExecutionPointerStart;//not accesible directly

	UInt32* mRegisterAddresses[REGISTER_COUNT];//register table

	MachineState mMachineState;

	Int32 mStackReverseBuffer[STACK_REVERSE_BUFFER_SIZE];

	FileHeader mFileHeader;
	std::vector<SymbolUsage> mSymbolUseList;
};

#endif //VIRTUALMACHINE_H