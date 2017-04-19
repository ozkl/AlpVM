using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpVM_Assembler
{
    public enum OpCode
    {
        NOOPERATION,
        HALT,
        PAUSE,
        PUSH,
        PUSHPART,
        PUSHALL,
        POP,
        POPPART,
        POPALL,
        ADD,
        SUB,
        MUL,
        DIV,
        MOD,
        NEGATIVE,
        SHIFTLEFT,
        SHIFTRIGHT,
        BITWISE_OR,
        BITWISE_AND,
        BITWISE_XOR,
        BITWISE_COMPLEMENT,
        CONVERT,
        ASSIGN,
        INC_BP,
        DEC_BP,
        INC_SP,
        DEC_SP,
        COMPARE,
        SETCC_EQ,
        SETCC_NEQ,
        SETCC_GT,
        SETCC_GTE,
        SETCC_LT,
        SETCC_LTE,
        JUMP,
        JUMPIF_EQ,
        JUMPIF_NEQ,
        JUMPIF_GT,
        JUMPIF_GTE,
        JUMPIF_LT,
        JUMPIF_LTE,
        CALL,
        RET,
        IRET,
        SET_INTERRUPT_HANDLER, //first parameter is Interrupt
        DISABLE_INTERRUPTS,
        ENABLE_INTERRUPTS,
        REVERSE_STACK,
        SET_COUNTER,
        READ_COUNTER,
        SET_MEMORY_ARRAY,
        COPY_MEMORY_ARRAY
    };

    [Flags]
    public enum OpModifier
    {
        Empty = 0,

        //Access-Type group
        Immediate = 1,
        Register = 2,
        Memory = 4,
        MemoryAtRegister = 8,

        //Access-Relativeness group
        BpOffset = 16,
        SpOffset = 32,

        //Jump-Relativeness group
        Relative = 64,
        Absolute = 128,

        //Data-Type group for arithmetic
        DataTypeI1 = 256,
        DataTypeI2 = 512,
        DataTypeI4 = 1024,
        DataTypeU1 = 2048,
        DataTypeU2 = 4096,
        DataTypeU4 = 8192,
        DataTypeF4 = 16384
    };

    //Size should be 16
    public class Instruction
    {
        public OpCode Operation;
        public UInt16 Modifier1;
        public UInt16 Modifier2;
        public Int32 Parameter1;
        public Int32 Parameter2;

        public Instruction(OpCode opCode = OpCode.NOOPERATION, OpModifier modifier1 = OpModifier.Empty, OpModifier modifier2 = OpModifier.Empty, Int32 parameter1 = 0, Int32 parameter2 = 0)
        {
            Operation = opCode;
            Modifier1 = (UInt16)modifier1;
            Modifier2 = (UInt16)modifier2;
            Parameter1 = parameter1;
            Parameter2 = parameter2;
        }

        public const int InstructionSize = 16;
    };

    public enum Register
    {
        BasePointer = 0,
        StackPointer = 1,
        Error = 2,
        Return = 3,
        Alp1 = 4,
        Alp2 = 5,
        Alp3 = 6,
        Alp4 = 7,
        Alp5 = 8,
        Alp6 = 9,
        Alp7 = 10,
        Alp8 = 11,
    };
}
