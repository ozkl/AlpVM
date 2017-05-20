@echo off
lcc\bin\cpp %1 | lcc\bin\rcc -target=bytecode > %1.asm