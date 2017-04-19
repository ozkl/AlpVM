#ifndef INSTRUCTION_H
#define INSTRUCTION_H

#include "Globals.h"

enum OpCode
{
	OC_NOOPERATION,
	OC_HALT,
	OC_PAUSE,
	OC_PUSH,
	OC_PUSHPART,
	OC_PUSHALL,
	OC_POP,
	OC_POPPART,
	OC_POPALL,
	OC_ADD,
	OC_SUB,
	OC_MUL,
	OC_DIV,
	OC_MOD,
	OC_NEGATIVE,
	OC_SHIFTLEFT,
	OC_SHIFTRIGHT,
	OC_BITWISE_OR,
	OC_BITWISE_AND,
	OC_BITWISE_XOR,
	OC_BITWISE_COMPLEMENT,
	OC_CONVERT,
	OC_ASSIGN,
	OC_INC_BP,
	OC_DEC_BP,
	OC_INC_SP,
	OC_DEC_SP,
	OC_COMPARE,
	OC_SETCC_EQ,
	OC_SETCC_NEQ,
	OC_SETCC_GT,
	OC_SETCC_GTE,
	OC_SETCC_LT,
	OC_SETCC_LTE,
	OC_JUMP,
	OC_JUMPIF_EQ,
	OC_JUMPIF_NEQ,
	OC_JUMPIF_GT,
	OC_JUMPIF_GTE,
	OC_JUMPIF_LT,
	OC_JUMPIF_LTE,
	OC_CALL,
	OC_RET,
	OC_IRET,
	OC_SET_INTERRUPT_HANDLER, //first parameter is Interrupt
	OC_DISABLE_INTERRUPTS,
	OC_ENABLE_INTERRUPTS,
	OC_REVERSE_STACK,
	OC_SET_COUNTER,
	OC_READ_COUNTER,
	OC_SET_MEMORY_ARRAY,
	OC_COPY_MEMORY_ARRAY
};

enum OpModifier
{
	OM_Empty = 0,

	//Access-Type group
	OM_Immediate = 1,
	OM_Register = 2,
	OM_Memory = 4,
	OM_MemoryAtRegister = 8,

	//Access-Relativeness gorup
	OM_BpOffset = 16,
	OM_SpOffset = 32,

	//Jump-Relativeness gorup
	OM_Relative = 64,
	OM_Absolute = 128,

	//Data-Type group for arithmetic
	OM_DataTypeI1 = 256,
	OM_DataTypeI2 = 512,
	OM_DataTypeI4 = 1024,
	OM_DataTypeU1 = 2048,
	OM_DataTypeU2 = 4096,
	OM_DataTypeU4 = 8192,
	OM_DataTypeF4 = 16384,
};

#define OPCODE_COUNT 43

//Size should be 16
struct Instruction
{
	OpCode Operation;
	UInt16 Modifier1;
	UInt16 Modifier2;
	Int32 Parameter1;
	Int32 Parameter2;

	Instruction(OpCode opCode = OC_NOOPERATION, OpModifier modifier1 = OM_Empty, OpModifier modifier2 = OM_Empty, Int32 parameter1 = 0, Int32 parameter2 = 0)
	{
		Operation = opCode;
		Modifier1 = modifier1;
		Modifier2 = modifier2;
		Parameter1 = parameter1;
		Parameter2 = parameter2;
	}

	static bool isModifierFlagActive(UInt16 modifier, OpModifier flag)
	{
		return ((modifier & flag) == flag);
	}
};

#endif //INSTRUCTION_H