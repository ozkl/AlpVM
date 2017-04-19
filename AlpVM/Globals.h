#ifndef GLOBALS_H
#define GLOBALS_H

typedef unsigned int UInt32;
typedef int Int32;
typedef unsigned short UInt16;
typedef short Int16;
typedef unsigned char UByte;
typedef char SByte;

enum ErrorCode
{
	Ec_NoError = 0,
	Ec_InvalidExecution = 1,
	Ec_InvalidInterupt = 2,
	Ec_InvalidOperation = 3
};

enum Interrupt
{
	I_NoInterrupt = 0,
	I_Timer = 1,
	I_Keyboard = 2
};
#define INTERRUPT_COUNT 3

enum Register
{
    Reg_BasePointer = 0,
    Reg_StackPointer = 1,
    Reg_Error = 2,
    Reg_Return = 3,
    Reg_Alp1 = 4,
    Reg_Alp2 = 5,
    Reg_Alp3 = 6,
	Reg_Alp4 = 7,
	Reg_Alp5 = 8,
	Reg_Alp6 = 9,
	Reg_Alp7 = 10,
	Reg_Alp8 = 11,
};

#define REGISTER_COUNT 12


struct MachineState
{
	bool mHalted;
	bool mPaused;
	bool mInterruptsEnabled;

	UByte mFlagZero;
	UByte mFlagSign;

	Interrupt mInterrupt;
	UInt32 mInterruptTable[INTERRUPT_COUNT];//must cover whole Interrupt enum

	//These are registers.
	UInt32 mExecutionPointer;//not accesible directly

	UInt32 mCounter;

	UInt32 mBasePointer;
	UInt32 mStackPointer;
	UInt32 mError;
	UInt32 mReturn;
	UInt32 mAlp1;
	UInt32 mAlp2;
	UInt32 mAlp3;
	UInt32 mAlp4;
	UInt32 mAlp5;
	UInt32 mAlp6;
	UInt32 mAlp7;
	UInt32 mAlp8;
};

#endif //GLOBALS_H