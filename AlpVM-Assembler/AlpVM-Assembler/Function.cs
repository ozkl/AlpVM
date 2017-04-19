using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlpVM_Assembler
{
    class Function
    {
        public Function(string name, int address, int localDataSize = 0, int argumentsCallDataSize = 0)
        {
            Name = name;
            Address = address;
            LocalDataSize = localDataSize;
            ArgumentsCallDataSize = argumentsCallDataSize;
        }

        public string Name
        {
            private set;
            get;
        }

        public int LocalDataSize
        {
            private set;
            get;
        }

        public int ArgumentsCallDataSize
        {
            private set;
            get;
        }

        public int Address
        {
            private set;
            get;
        }
    }
}
