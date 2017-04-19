using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpVM_Debugger
{
    public class Machine
    {
        public MachineState MachineState;

        byte[] mMachineMemory;

        public Machine()
        {
            mMachineMemory = new byte[64 * 1024 * 1024];
        }

        public byte[] MachineMemory
        {
            get
            {
                return mMachineMemory;
            }
        }
    }
}
