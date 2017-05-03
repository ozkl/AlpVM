# Interrupts
Interrupts are used for receiving hardware events. Hardware generates an interrupt for a particular event detected by the hardware. In software side, if it is intended to take that action, an interrupt handler is set to receive that interrupt. 

To illustrate this scenario, keyboard chip generates interrupt to inform a key press event. If an interrupt handler is assigned for keyboard key press interrupt, CPU jumps to the handler. When handler finishes its job, CPU returns where it was.

Interrupts can be disabled or enabled by related instructions. Also, CPU disables interrupts before calling an interrupt handler. When returning from an interrupt handler, IRET instruction enables interrupts. Interrupts are disabled during handling an interrupt.

Interrupt handlers are assigned with SET_INTERRUPT_HANDLER instruction. First parameter to this instruction is interrupt type. Second parameter is the register containing the address of interrupt handler subroutine.

There are currently two interrupts:

|Id |Description|
|---|-----------|
|1  |Timer
|2  |Keyboard
