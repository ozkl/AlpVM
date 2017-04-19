using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AlpVM_Debugger
{
    public enum Interrupt
    {
        I_NoInterrupt = 0,
        I_Timer = 1,
        I_Keyboard = 2
    };

    [StructLayout(LayoutKind.Sequential, Pack =4)]
    public struct MachineState
    {
        public byte mHalted; //bool
        public byte mPaused; //bool
        public byte mInterruptsEnabled; //bool

        public byte mFlagZero;
        public byte mFlagSign;

        public Interrupt mInterrupt;
        public UInt32 mInterruptTable0;
        public UInt32 mInterruptTable1;
        public UInt32 mInterruptTable2;

        //These are registers.
        public UInt32 mExecutionPointer;//not accesible directly

        public UInt32 mCounter;

        public UInt32 mBasePointer;
        public UInt32 mStackPointer;
        public UInt32 mError;
        public UInt32 mReturn;
        public UInt32 mAlp1;
        public UInt32 mAlp2;
        public UInt32 mAlp3;
        public UInt32 mAlp4;
        public UInt32 mAlp5;
        public UInt32 mAlp6;
        public UInt32 mAlp7;
        public UInt32 mAlp8;
    };
}
