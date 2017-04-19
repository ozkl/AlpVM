#include <iostream>
#include <fstream>

#include "Instruction.h"
#include "Memory.h"
#include "SystemTime.h"
#include "TimerDevice.h"
#include "KeyboardDevice.h"

#include "VirtualMachine.h"

static const UInt32 MEMORY_SIZE = 8 * 1024 * 1024;


static const UInt32 DEV_VIDEO = 1024;
static const UInt32 DEV_TIMER = 512;
static const UInt32 DEV_SYSTEMTIME = 500;
static const UInt32 DEV_KEYBOARD = 800;

VirtualMachine::VirtualMachine()
{
	mMemory = new Memory(MEMORY_SIZE);
	mTimerDevice = new TimerDevice((UInt32*)(mMemory->getRawData() + DEV_TIMER));
	mKeyboardDevice = new KeyboardDevice((UInt32*)(mMemory->getRawData() + DEV_KEYBOARD));
	mSystemTime = new SystemTime((UInt32*)(mMemory->getRawData() + DEV_SYSTEMTIME));

	mLoadedProgramInstructionCount = 0;

	clearMemory();

	restart();
}

VirtualMachine::~VirtualMachine()
{
	delete mSystemTime;
	delete mKeyboardDevice;
	delete mTimerDevice;
	delete mMemory;
}

void VirtualMachine::restart()
{
	mMachineState.mHalted = false;
	mMachineState.mPaused = false;
	mMachineState.mInterruptsEnabled = true;

	mMachineState.mFlagSign = 0;
	mMachineState.mFlagZero = 0;

	mMachineState.mInterrupt = I_NoInterrupt;

	mMachineState.mInterruptTable[0] = 0;
	mMachineState.mInterruptTable[1] = 0;
	mMachineState.mInterruptTable[2] = 0;

	mExecutionPointerStart = 2*1024*1024;//2 * 65536;//16384;

	mMachineState.mCounter = 0;

	mMachineState.mExecutionPointer = mExecutionPointerStart;
	mMachineState.mBasePointer = 1024*1024;//65536;//32768;
	mMachineState.mStackPointer = mMachineState.mBasePointer;
	mMachineState.mError = 0;
	mMachineState.mReturn = 0;
	mMachineState.mAlp1 = 0;
	mMachineState.mAlp2 = 0;
	mMachineState.mAlp3 = 0;
	mMachineState.mAlp4 = 0;
	mMachineState.mAlp5 = 0;
	mMachineState.mAlp6 = 0;
	mMachineState.mAlp7 = 0;
	mMachineState.mAlp8 = 0;

	mRegisterAddresses[Reg_BasePointer] = &mMachineState.mBasePointer;
	mRegisterAddresses[Reg_StackPointer] = &mMachineState.mStackPointer;
	mRegisterAddresses[Reg_Error] = &mMachineState.mError;
	mRegisterAddresses[Reg_Return] = &mMachineState.mReturn;
	mRegisterAddresses[Reg_Alp1] = &mMachineState.mAlp1;
	mRegisterAddresses[Reg_Alp2] = &mMachineState.mAlp2;
	mRegisterAddresses[Reg_Alp3] = &mMachineState.mAlp3;
	mRegisterAddresses[Reg_Alp4] = &mMachineState.mAlp4;
	mRegisterAddresses[Reg_Alp5] = &mMachineState.mAlp5;
	mRegisterAddresses[Reg_Alp6] = &mMachineState.mAlp6;
	mRegisterAddresses[Reg_Alp7] = &mMachineState.mAlp7;
	mRegisterAddresses[Reg_Alp8] = &mMachineState.mAlp8;
}

bool VirtualMachine::openFromFile(const char* file)
{
	const int sizeInstruction = sizeof(Instruction);

	mSymbolUseList.clear();

	std::ifstream inFile(file, std::ios_base::binary);
	if (inFile.is_open())
	{
		UInt32 address = mExecutionPointerStart;

		inFile.read((char*)&mFileHeader, sizeof(FileHeader));

		inFile.read((char*)mMemory->getRawData() + address, mFileHeader.CodeSize);
		address += mFileHeader.CodeSize;

		inFile.read((char*)mMemory->getRawData() + address, mFileHeader.DataSize);
		address += mFileHeader.DataSize;

		inFile.read((char*)mMemory->getRawData() + address, mFileHeader.LitSize);
		address += mFileHeader.LitSize;

		if (mFileHeader.SymbolUsageTableSize > 0)
        {
            int counter = (int)mFileHeader.SymbolUsageTableSize;

            while (counter > 0)
            {
                SymbolUsage symbolUsage = SymbolUsage::deserialize(inFile);
                counter -= symbolUsage.calculateSize();
                mSymbolUseList.push_back(symbolUsage);
            }
        }

		memset(mMemory->getRawData() + address, 0, mFileHeader.BssSize);

		mLoadedProgramInstructionCount = mFileHeader.CodeSize / sizeInstruction;
		
		inFile.close();

		relocateSymbols();

		return true;
	}
	return false;
}

void VirtualMachine::relocateSymbols()
{
	//Position Indipendent Magic goes here below

	int symbolCount = mSymbolUseList.size();
	for (int i = 0; i < symbolCount; ++i)
	{
		const SymbolUsage& su = mSymbolUseList[i];
		switch (su.Section)
		{
		case SymbolUsage::EnSection::Code:
			{
				Instruction* instruction = (Instruction*)((char*)mMemory->getRawData() + mExecutionPointerStart);
				instruction[su.Address].Parameter1 += mExecutionPointerStart;
			}
			break;
		case SymbolUsage::EnSection::Data:
			{
				char* dataSection = (char*)mMemory->getRawData() + mExecutionPointerStart + mFileHeader.CodeSize;
				int* address = (int*)(dataSection + su.Address);
				*address += mExecutionPointerStart;
			}
			break;
		}
	}
}

MachineState* VirtualMachine::getMachineState()
{
	return &mMachineState;
}

void VirtualMachine::clearMemory()
{
	mMemory->clear();

	mLoadedProgramInstructionCount = 0;
}

UByte* VirtualMachine::getMemory()
{
	return mMemory->getRawData();
}

UByte* VirtualMachine::getVideoMemory()
{
	return mMemory->getRawData() + DEV_VIDEO;
}

void VirtualMachine::interrupt(Interrupt i)
{
	mMachineState.mInterrupt = i;
}

void VirtualMachine::sendKey(UInt32 keyCode)
{
	mKeyboardDevice->setKey(keyCode);

	interrupt(I_Keyboard);
}

void VirtualMachine::setRegisterValue(UInt32 index, UInt32 value)
{
	(*mRegisterAddresses[index]) = value;
}

UInt32 VirtualMachine::getRegisterValue(UInt32 index) const
{
	return *mRegisterAddresses[index];
}

void VirtualMachine::pushReg(UInt32 index)
{
	push(getRegisterValue(index));
}

void VirtualMachine::pushImmediate(Int32 value)
{
	push(value);
}

void VirtualMachine::popReg(UInt32 index)
{
	setRegisterValue(index, pop());
}

void VirtualMachine::push(Int32 data)
{
	mMachineState.mStackPointer += sizeof(UInt32);
	mMemory->setData<UInt32>(mMachineState.mStackPointer, data);
}

Int32 VirtualMachine::pop()
{
	Int32 data = 0;
	mMemory->getData<Int32>(mMachineState.mStackPointer, data);
	mMachineState.mStackPointer -= sizeof(Int32);

	return data;
}

void VirtualMachine::pushPart()
{
	pushReg(3);//Reg_Return
	pushReg(4);
	pushReg(5);
	pushReg(6);
	pushReg(7);
	pushReg(8);
	pushReg(9);
	pushReg(10);
	pushReg(11);//Reg_Alp8
}

void VirtualMachine::popPart()
{
	popReg(11);//Reg_Alp8
	popReg(10);
	popReg(9);
	popReg(8);
	popReg(7);
	popReg(6);
	popReg(5);
	popReg(4);
	popReg(3);//Reg_Return
}

void VirtualMachine::pushAll()
{
	pushReg(3);//Reg_Return
	pushReg(4);
	pushReg(5);
	pushReg(6);
	pushReg(7);
	pushReg(8);
	pushReg(9);
	pushReg(10);
	pushReg(11);//Reg_Alp8
	pushReg(0);//Reg_BasePointer
}

void VirtualMachine::popAll()
{
	popReg(0);//Reg_BasePointer
	popReg(11);//Reg_Alp8
	popReg(10);
	popReg(9);
	popReg(8);
	popReg(7);
	popReg(6);
	popReg(5);
	popReg(4);
	popReg(3);//Reg_Return
}

Int32 VirtualMachine::readValue(UInt32 imOrRegOrMem, UInt16 flags)
{
	Int32 data = 0;
	if (Instruction::isModifierFlagActive(flags, OM_Immediate))
	{
		data = imOrRegOrMem;
	}
	else if (Instruction::isModifierFlagActive(flags, OM_Register))
	{
		data = getRegisterValue(imOrRegOrMem);
	}
	else if (Instruction::isModifierFlagActive(flags, OM_Memory) ||
		Instruction::isModifierFlagActive(flags, OM_MemoryAtRegister))
	{
		data = readMemValue(imOrRegOrMem, flags);
	}

	return data;
}

void VirtualMachine::writeValue(UInt32 regOrMem, UInt16 flags, Int32 data)
{
	if (Instruction::isModifierFlagActive(flags, OM_Register))
	{
		setRegisterValue(regOrMem, data);
	}
	else if (Instruction::isModifierFlagActive(flags, OM_Memory) ||
		Instruction::isModifierFlagActive(flags, OM_MemoryAtRegister))
	{
		writeMemValue(regOrMem, flags, data);
	}
	else if (Instruction::isModifierFlagActive(flags, OM_Immediate))
	{
		mMachineState.mError = Ec_InvalidOperation;
	}
}

Int32 VirtualMachine::readMemValue(UInt32 addressOrRegister, UInt16 flags)
{
	Int32 result = 0;
	bool directMemory = Instruction::isModifierFlagActive(flags, OM_Memory);
	bool memoryAtRegister = Instruction::isModifierFlagActive(flags, OM_MemoryAtRegister);

	if (directMemory || memoryAtRegister)
	{
		UInt32 memAddress = addressOrRegister;
		if (memoryAtRegister)
		{
			memAddress = getRegisterValue(addressOrRegister);
		}

		if (Instruction::isModifierFlagActive(flags, OM_BpOffset))
		{
			memAddress += mMachineState.mBasePointer;
		}
		else if (Instruction::isModifierFlagActive(flags, OM_SpOffset))
		{
			memAddress += mMachineState.mStackPointer;
		}

		mMemory->getData<Int32>(memAddress, result);

		if (Instruction::isModifierFlagActive(flags, OM_DataTypeI1) || Instruction::isModifierFlagActive(flags, OM_DataTypeU1))
		{
			result = (result & 0x000000FF);
		}
		else if (Instruction::isModifierFlagActive(flags, OM_DataTypeI2) || Instruction::isModifierFlagActive(flags, OM_DataTypeU2))
		{
			result = (result & 0x0000FFFF);
		}
	}

	return result;
}

void VirtualMachine::writeMemValue(UInt32 addressOrRegister, UInt16 flags, Int32 data)
{
	bool directMemory = Instruction::isModifierFlagActive(flags, OM_Memory);
	bool memoryAtRegister = Instruction::isModifierFlagActive(flags, OM_MemoryAtRegister);

	if (directMemory || memoryAtRegister)
	{
		UInt32 memAddress = addressOrRegister;
		if (memoryAtRegister)
		{
			memAddress = getRegisterValue(addressOrRegister);
		}

		if (Instruction::isModifierFlagActive(flags, OM_BpOffset))
		{
			memAddress += mMachineState.mBasePointer;
		}
		else if (Instruction::isModifierFlagActive(flags, OM_SpOffset))
		{
			memAddress += mMachineState.mStackPointer;
		}

		if (Instruction::isModifierFlagActive(flags, OM_DataTypeI1) || Instruction::isModifierFlagActive(flags, OM_DataTypeU1))
		{
			int value = (data & 0x000000FF);

			mMemory->setData<UByte>(memAddress, (UByte)value);
		}
		else if (Instruction::isModifierFlagActive(flags, OM_DataTypeI2) || Instruction::isModifierFlagActive(flags, OM_DataTypeU2))
		{
			int value = (data & 0x0000FFFF);

			mMemory->setData<UInt16>(memAddress, (UInt16)value);
		}
		else
		{
			mMemory->setData<Int32>(memAddress, data);
		}
	}
}

bool VirtualMachine::isJumpAddressValid(UInt32 address)
{
	if (address % sizeof(Instruction) != 0)
	{
		mMachineState.mError = Ec_InvalidExecution;

		return false;
	}

	return true;
}

UInt32 VirtualMachine::getJumpAddress(UInt32 imOrRegOrMem, UInt16 flags)
{
	Int32 data = readValue(imOrRegOrMem, flags);

	Int32 address = mMachineState.mExecutionPointer;
	if (Instruction::isModifierFlagActive(flags, OM_Relative))
	{
		address += data;
	}
	else if (Instruction::isModifierFlagActive(flags, OM_Absolute))
	{
		address = data;
	}

	return address;
}

UInt32 VirtualMachine::jump(UInt32 address)
{
	if (isJumpAddressValid(address))
	{
		mMachineState.mExecutionPointer = /*mExecutionPointerStart +*/ address;
	}

	return mMachineState.mExecutionPointer;
}

UInt32 VirtualMachine::jumpSmart(UInt32 imOrRegOrMem, UInt16 flags)
{
	UInt32 address = getJumpAddress(imOrRegOrMem, flags);

	return jump(address);
}

template< class T >
void compareTemplate(MachineState* state, T value1, T value2)
{
	T temp = value1 - value2;
	if (0 == temp)
	{
		state->mFlagZero = 1;
	}
	else
	{
		state->mFlagZero = 0;
	}

	if (temp < 0)
	{
		state->mFlagSign = 1;
	}
	else
	{
		state->mFlagSign = 0;
	}
}

void VirtualMachine::compare(Instruction& ins)
{
	Int32 value1 = readValue(ins.Parameter1, ins.Modifier1);
	Int32 value2 = readValue(ins.Parameter2, ins.Modifier2);

	//DataType of Modifier1 determines the whole operation type

	if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI4))
	{
		compareTemplate(&mMachineState, value1, value2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU4))
	{
		compareTemplate(&mMachineState, (UInt32)value1, (UInt32)value2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeF4))
	{
		float* fValue1 = (float*)&value1;
		float* fValue2 = (float*)&value2;

		compareTemplate(&mMachineState, *fValue1, *fValue2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI2))
	{
		compareTemplate(&mMachineState, (Int16)value1, (Int16)value2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU2))
	{
		compareTemplate(&mMachineState, (UInt16)value1, (UInt16)value2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI1))
	{
		compareTemplate(&mMachineState, (SByte)value1, (SByte)value2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU1))
	{
		compareTemplate(&mMachineState, (UByte)value1, (UByte)value2);
	}
	else
	{
		compareTemplate(&mMachineState, value1, value2);
	}
}

template< class T >
T getArithmaticResult(MachineState* state, OpCode operation, T value1, T value2)
{
	T result = 0;
	switch (operation)
	{
	case OC_ADD:
		result = value1 + value2;
	break;
	case OC_SUB:
		result = value1 - value2;
	break;
	case OC_MUL:
		result = value1 * value2;
	break;
	case OC_DIV:
		result = value1 / value2;
	break;
	case OC_MOD:
		result = value1 % value2;
	break;
	case OC_SHIFTLEFT:
		result = value1 << value2;
		break;
	case OC_SHIFTRIGHT:
		result = value1 >> value2;
		break;
	case OC_BITWISE_OR:
		result = value1 | value2;
	break;
	case OC_BITWISE_AND:
		result = value1 & value2;
	break;
	case OC_BITWISE_XOR:
		result = value1 ^ value2;
		break;
	default:
		state->mError = Ec_InvalidOperation;
		break;
	}

	return result;
}

float getArithmaticResultF(MachineState* state, OpCode operation, float value1, float value2)
{
	float result = 0;
	switch (operation)
	{
	case OC_ADD:
		result = value1 + value2;
		break;
	case OC_SUB:
		result = value1 - value2;
		break;
	case OC_MUL:
		result = value1 * value2;
		break;
	case OC_DIV:
		result = value1 / value2;
		break;
	default:
		state->mError = Ec_InvalidOperation;
		break;
	}

	return result;
}

template< class T >
T getComplementResult(T value)
{
	return ~value;
}

void VirtualMachine::doArithmatic(Instruction& ins)
{
	Int32 value1 = readValue(ins.Parameter1, ins.Modifier1);
	Int32 value2 = readValue(ins.Parameter2, ins.Modifier2);
	Int32 result = 0;

	//DataType of Modifier1 determines the whole operation type

	if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI4))
	{
		result = getArithmaticResult(&mMachineState, ins.Operation, value1, value2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU4))
	{
		result = getArithmaticResult(&mMachineState, ins.Operation, (UInt32)value1, (UInt32)value2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeF4))
	{
		float* fValue1 = (float*)&value1;
		float* fValue2 = (float*)&value2;
		float* fResult = (float*)&result;
		*fResult = getArithmaticResultF(&mMachineState, ins.Operation, *fValue1, *fValue2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI2))
	{
		result = getArithmaticResult(&mMachineState, ins.Operation, (Int16)value1, (Int16)value2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU2))
	{
		result = getArithmaticResult(&mMachineState, ins.Operation, (UInt16)value1, (UInt16)value2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI1))
	{
		result = getArithmaticResult(&mMachineState, ins.Operation, (SByte)value1, (SByte)value2);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU1))
	{
		result = getArithmaticResult(&mMachineState, ins.Operation, (UByte)value1, (UByte)value2);
	}
	else
	{
		result = getArithmaticResult(&mMachineState, ins.Operation, value1, value2);
	}

	writeValue(ins.Parameter1, ins.Modifier1, result);
}

void VirtualMachine::doComplement(Instruction& ins)
{
	Int32 value1 = readValue(ins.Parameter1, ins.Modifier1);

	Int32 result = 0;

	if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI4))
	{
		result = getComplementResult(value1);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU4))
	{
		result = getComplementResult((UInt32)value1);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI2))
	{
		result = getComplementResult((Int16)value1);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU2))
	{
		result = getComplementResult((UInt16)value1);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI1))
	{
		result = getComplementResult((SByte)value1);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU1))
	{
		result = getComplementResult((UByte)value1);
	}
	else
	{
		result = getComplementResult(value1);
	}

	writeValue(ins.Parameter1, ins.Modifier1, result);
}

template< class T >
T getNegative(T value)
{
	return -value;
}

void VirtualMachine::doNegative(Instruction& ins)
{
	Int32 value1 = readValue(ins.Parameter1, ins.Modifier1);

	Int32 result = 0;

	if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI4) ||
		Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU4))
	{
		result = getNegative(value1);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeF4))
	{
		float* fValue1 = (float*)&value1;
		float* fResult = (float*)&result;
		*fResult = getNegative(*fValue1);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI2) ||
		Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU2))
	{
		result = getNegative((Int16)value1);
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI1) ||
		Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU1))
	{
		result = getNegative((SByte)value1);
	}
	else
	{
		result = getNegative(value1);
	}

	writeValue(ins.Parameter1, ins.Modifier1, result);
}

void VirtualMachine::doConvert(Instruction& ins)
{
	Int32 value2 = readValue(ins.Parameter2, ins.Modifier2);
	Int32 result = 0;


	if (Instruction::isModifierFlagActive(ins.Modifier2, OM_DataTypeI4) ||
		Instruction::isModifierFlagActive(ins.Modifier2, OM_DataTypeU4))
	{
		if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI4) ||
			Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU4))
		{
			result = value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeF4))
		{
			float f = (float)value2;
			float* fValue2 = &f;
			result = *((Int32*)fValue2);
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI2))
		{
			result = (Int16)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU2))
		{
			result = (UInt16)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI1))
		{
			result = (SByte)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU1))
		{
			result = (UByte)value2;
		}
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier2, OM_DataTypeF4))
	{
		float* fValue2 = (float*)&value2;

		if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI4) ||
			Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU4))
		{
			result = (Int32)*fValue2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeF4))
		{
			result = value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI2))
		{
			result = (Int16)*fValue2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU2))
		{
			result = (UInt16)*fValue2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI1))
		{
			result = (SByte)*fValue2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU1))
		{
			result = (UByte)*fValue2;
		}
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier2, OM_DataTypeI2) ||
		Instruction::isModifierFlagActive(ins.Modifier2, OM_DataTypeU2))
	{
		if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI4))
		{
			result = (Int32)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU4))
		{
			result = (UInt32)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeF4))
		{
			float f = (float)value2;
			float* fValue2 = &f;
			result = *((Int32*)fValue2);
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI2) ||
			Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU2))
		{
			result = (Int16)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI1))
		{
			result = (SByte)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU1))
		{
			result = (UByte)value2;
		}
	}
	else if (Instruction::isModifierFlagActive(ins.Modifier2, OM_DataTypeI1) ||
		Instruction::isModifierFlagActive(ins.Modifier2, OM_DataTypeU1))
	{
		if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI4))
		{
			result = (Int32)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU4))
		{
			result = (UInt32)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeF4))
		{
			float f = (float)value2;
			float* fValue2 = &f;
			result = *((Int32*)fValue2);
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI2))
		{
			result = (Int16)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU2))
		{
			result = (UInt16)value2;
		}
		else if (Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeI1) ||
			Instruction::isModifierFlagActive(ins.Modifier1, OM_DataTypeU1))
		{
			result = (SByte)value2;
		}
	}

	writeValue(ins.Parameter1, ins.Modifier1, result);
}

UInt32 VirtualMachine::executeInstruction()
{
	if (isHalted())
	{
		return mMachineState.mExecutionPointer;
	}

	mSystemTime->process();

	mTimerDevice->process();

	mMachineState.mError = Ec_NoError;

	if (mTimerDevice->isOccuredGlobal())
	{
		mTimerDevice->clearOccuredGlobal();

		mMachineState.mInterrupt = I_Timer;
	}


	if (I_NoInterrupt != mMachineState.mInterrupt && mMachineState.mInterruptsEnabled)
	{
		if (mMachineState.mInterruptTable[mMachineState.mInterrupt] != 0)//if handler exists
		{
			//Call handler mInterruptTable[mInterrupt]

			//Interrupt handler must return with IRET so that interrupts can be enabled again.
			mMachineState.mInterruptsEnabled = false;

			pushPart();

			//This is equalivent to CALL
			mMachineState.mStackPointer += sizeof(UInt32);
			mMemory->setData<UInt32>(mMachineState.mStackPointer, mMachineState.mExecutionPointer);
			mMachineState.mExecutionPointer = mExecutionPointerStart + mMachineState.mInterruptTable[mMachineState.mInterrupt];
		}

		mMachineState.mInterrupt = I_NoInterrupt;
	}
	
	Instruction ins;
	mMachineState.mExecutionPointer = mMemory->getData<Instruction>(mMachineState.mExecutionPointer, ins);

	switch (ins.Operation)
	{
	case OC_NOOPERATION:
		{
		}
		break;
	case OC_HALT:
		{
			mMachineState.mHalted = true;
		}
		break;
	case OC_PAUSE:
		{
			mMachineState.mPaused = true;
		}
		break;
	case OC_PUSH:
		{
			Int32 value = readValue(ins.Parameter1, ins.Modifier1);
			push(value);
		}
		break;
	case OC_PUSHPART:
		{
			pushPart();
		}
		break;
	case OC_PUSHALL:
		{
			pushAll();
		}
		break;
	case OC_POP:
		{
			Int32 value = pop();
			writeValue(ins.Parameter1, ins.Modifier1, value);
		}
		break;
	case OC_POPPART:
		{
			popPart();
		}
		break;
	case OC_POPALL:
		{
			popAll();
		}
		break;
	case OC_ADD:
	case OC_SUB:
	case OC_MUL:
	case OC_DIV:
	case OC_MOD:
	case OC_SHIFTLEFT:
	case OC_SHIFTRIGHT:
	case OC_BITWISE_OR:
	case OC_BITWISE_AND:
	case OC_BITWISE_XOR:
		doArithmatic(ins);
		break;
	case OC_BITWISE_COMPLEMENT:
		doComplement(ins);
		break;
	case OC_NEGATIVE:
		doNegative(ins);
		break;
	case OC_CONVERT:
		doConvert(ins);
		break;
	case OC_ASSIGN:
		{
			//Read
			Int32 data = readValue(ins.Parameter2, ins.Modifier2);

			//Write
			writeValue(ins.Parameter1, ins.Modifier1, data);
		}
		break;
	case OC_INC_BP:
		{
			mMachineState.mBasePointer += readValue(ins.Parameter1, ins.Modifier1);
		}
		break;
	case OC_DEC_BP:
		{
			mMachineState.mBasePointer -= readValue(ins.Parameter1, ins.Modifier1);
		}
		break;
	case OC_INC_SP:
		{
			mMachineState.mStackPointer += readValue(ins.Parameter1, ins.Modifier1);
		}
		break;
	case OC_DEC_SP:
		{
			mMachineState.mStackPointer -= readValue(ins.Parameter1, ins.Modifier1);
		}
		break;
	case OC_COMPARE:
		{
			compare(ins);
		}
		break;
	case OC_SETCC_EQ:
		{
			if (1 == mMachineState.mFlagZero)
			{
				writeValue(ins.Parameter1, ins.Modifier1, 1);
			}
			else
			{
				writeValue(ins.Parameter1, ins.Modifier1, 0);
			}
		}
		break;
	case OC_SETCC_NEQ:
		{
			if (0 == mMachineState.mFlagZero)
			{
				writeValue(ins.Parameter1, ins.Modifier1, 1);
			}
			else
			{
				writeValue(ins.Parameter1, ins.Modifier1, 0);
			}
		}
		break;
	case OC_SETCC_GT:
		{
			if (0 == mMachineState.mFlagZero && 0 == mMachineState.mFlagSign)
			{
				writeValue(ins.Parameter1, ins.Modifier1, 1);
			}
			else
			{
				writeValue(ins.Parameter1, ins.Modifier1, 0);
			}
		}
		break;
	case OC_SETCC_GTE:
		{
			if (0 == mMachineState.mFlagSign)
			{
				writeValue(ins.Parameter1, ins.Modifier1, 1);
			}
			else
			{
				writeValue(ins.Parameter1, ins.Modifier1, 0);
			}
		}
		break;
	case OC_SETCC_LT:
		{
			if (0 == mMachineState.mFlagZero && 1 == mMachineState.mFlagSign)
			{
				writeValue(ins.Parameter1, ins.Modifier1, 1);
			}
			else
			{
				writeValue(ins.Parameter1, ins.Modifier1, 0);
			}
		}
		break;
	case OC_SETCC_LTE:
		{
			if (1 == mMachineState.mFlagSign)
			{
				writeValue(ins.Parameter1, ins.Modifier1, 1);
			}
			else
			{
				writeValue(ins.Parameter1, ins.Modifier1, 0);
			}
		}
		break;
	case OC_JUMP:
		{
			jumpSmart(ins.Parameter1, ins.Modifier1);
		}
		break;
	case OC_JUMPIF_EQ:
		{
			if (1 == mMachineState.mFlagZero)
			{
				jumpSmart(ins.Parameter1, ins.Modifier1);
			}
		}
		break;
	case OC_JUMPIF_NEQ:
		{
			if (0 == mMachineState.mFlagZero)
			{
				jumpSmart(ins.Parameter1, ins.Modifier1);
			}
		}
		break;
	case OC_JUMPIF_GT:
		{
			if (0 == mMachineState.mFlagZero && 0 == mMachineState.mFlagSign)
			{
				jumpSmart(ins.Parameter1, ins.Modifier1);
			}
		}
		break;
	case OC_JUMPIF_GTE:
		{
			if (0 == mMachineState.mFlagSign)
			{
				jumpSmart(ins.Parameter1, ins.Modifier1);
			}
		}
		break;
	case OC_JUMPIF_LT:
		{
			if (0 == mMachineState.mFlagZero && 1 == mMachineState.mFlagSign)
			{
				jumpSmart(ins.Parameter1, ins.Modifier1);
			}
		}
		break;
	case OC_JUMPIF_LTE:
		{
			if (1 == mMachineState.mFlagSign)
			{
				jumpSmart(ins.Parameter1, ins.Modifier1);
			}
		}
		break;
	case OC_CALL:
		{
			UInt32 address = getJumpAddress(ins.Parameter1, ins.Modifier1);

			if (isJumpAddressValid(address))
			{
				mMachineState.mStackPointer += sizeof(UInt32);
				mMemory->setData<UInt32>(mMachineState.mStackPointer, mMachineState.mExecutionPointer);
				
				jump(address);
			}
		}
		break;
	case OC_RET:
		{
			mMemory->getData<UInt32>(mMachineState.mStackPointer, mMachineState.mExecutionPointer);
			mMachineState.mStackPointer -= sizeof(UInt32);
		}
		break;
	case OC_IRET:
		{
			mMachineState.mInterruptsEnabled = true;
			mMemory->getData<UInt32>(mMachineState.mStackPointer, mMachineState.mExecutionPointer);
			mMachineState.mStackPointer -= sizeof(UInt32);

			popPart();
		}
		break;
	case OC_SET_INTERRUPT_HANDLER:
		{
			UInt32 interruptType = (ins.Parameter1);
			if (interruptType >= INTERRUPT_COUNT)
			{
				mMachineState.mError = Ec_InvalidInterupt;
				return mMachineState.mExecutionPointer;
			}

			UInt32 functionAddress = getJumpAddress(ins.Parameter2, ins.Modifier2);
			if (isJumpAddressValid(functionAddress))
			{
				mMachineState.mInterruptTable[interruptType] = functionAddress;
			}
		}
		break;
	case OC_DISABLE_INTERRUPTS:
		{
			mMachineState.mInterruptsEnabled = false;
		}
		break;
	case OC_ENABLE_INTERRUPTS:
		{
			mMachineState.mInterruptsEnabled = true;
		}
		break;
	case OC_REVERSE_STACK:
		{
			int index = readValue(ins.Parameter1, ins.Modifier1);
			int count = readValue(ins.Parameter2, ins.Modifier2);

			for (int i = index; i < count + index; ++i)
			{
				mMemory->getData<Int32>(mMachineState.mStackPointer - i*4, mStackReverseBuffer[i - index]);
			}

			for (int i = index; i < count + index; ++i)
			{
				mMemory->setData<Int32>(mMachineState.mStackPointer - i*4, mStackReverseBuffer[count - i - 1]);
			}
		}
		break;
	case OC_SET_COUNTER:
		{
			mMachineState.mCounter = readValue(ins.Parameter1, ins.Modifier1);
		}
		break;
	case OC_READ_COUNTER:
		{
			writeValue(ins.Parameter1, ins.Modifier1, mMachineState.mCounter);
		}
		break;
	case OC_SET_MEMORY_ARRAY:
		{
			if (mMachineState.mCounter > 0)
			{
				UInt32 address = readValue(ins.Parameter1, ins.Modifier1);
				Int32 value = readValue(ins.Parameter2, ins.Modifier2);
				memset(mMemory->getRawData() + address, value, mMachineState.mCounter);
				mMachineState.mCounter = 0;
			}
		}
		break;
	case OC_COPY_MEMORY_ARRAY:
		{
			if (mMachineState.mCounter > 0)
			{
				UInt32 address = readValue(ins.Parameter1, ins.Modifier1);
				UInt32 source = readValue(ins.Parameter2, ins.Modifier2);
				memcpy(mMemory->getRawData() + address, mMemory->getRawData() + source, mMachineState.mCounter);
				mMachineState.mCounter = 0;
			}
		}
		break;
	}

	return mMachineState.mExecutionPointer;
}
