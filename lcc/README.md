Bytecode backend of LCC is used for compiling C source files into the AlpVM assembly.
Refer to AlpVM assembler and linker for more information on building for AlpVM.

LCC source code here has small modifications for two reasons:
* To make double type 4 bytes long (to do this, we use bytecode.c from id software's Quake 3)
* To make LCC source compilable under Visual Studio 2015 (outp replaced)