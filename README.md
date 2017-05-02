# Summary
AlpVM is a simple virtual machine I developed in 2013 just for fun. It wasn't easy to write programs for it in its own machine language. So, I decided to make some high level languages available to it.
Now, it is possible to program it in C. To achieve this, I also write an assembler and a linker. For C compiler I used LCC. Assembler, and linker are written in C#. AlpVm is written in C++.

# AlpVM
AlpVM has a simple interrupt mechanism and also video, keyboard, and timer devices all of which are controlled by memory mapped I/O.
Since it is a simple machine, it has no memory protection support. So the code runs on it can modify itself.
Assembler and linker produce executables containing symbols (like an ELF or PE file) so that AlpVM can make relocation to achieve position independent code. Linker also makes relocations when combining object files to produce an executable.
Instructions of AlpVM are fixed-sized (16 bytes).

Note that, this is a hobby project so codes here are not expected to be the most efficient.
