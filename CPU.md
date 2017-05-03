# CPU
There are 52 instructions in AlpVM for now. Each instruction is 16 bytes long and consists of 4 byte opcode, 2 byte modifier1, 2 byte modifier2, 4 byte parameter1, and 4 byte parameter2.
Modifier1 and modifier2 affect parameter1 and parameter2 respectivly.
Modifiers are used for declaring a parameter is immediate/register/memory, its offset is relative/absolute, or its data size.

### Instruction Table
|Op |Instruction            |Parameter1|Parameter2|Description|
|---|-----------------------|:--------:|:--------:|-----------|
|0  |NOOPERATION            |-         |-         |Does nothing.
|1  |HALT                   |-         |-         |Halts the system. No instructions after this will run.
|2  |PAUSE                  |-         |-         |Pauses system.
|3  |PUSH                   |x         |-         |Pushes the P1 to the stack. Stack pointer is incremented by 4 byte before this operation. Note: The stack of AlpVM grows up. The stack grows from lower addresses to higher addresses.
|4  |PUSHPART               |-         |-         |Pushes values of the following registers to the stack with order: Return, Alp1, Alp2, Alp3, Alp4, Alp5, Alp6, Alp7, and Alp8.
|5  |PUSHALL                |-         |-         |Pushes values of the following registers to the stack with order: Return, Alp1, Alp2, Alp3, Alp4, Alp5, Alp6, Alp7, Alp8, and Base Pointer.
|6  |POP                    |x         |-         |Pops the value from the stack and assigns it to the P1. Stack pointer is decremented by 4 byte after this operation.
|7  |POPPART                |-         |-         |
|8  |POPALL                 |-         |-         |
|9  |ADD                    |-         |-         |
|10 |SUB                    |-         |-         |
|11 |MUL                    |-         |-         |
|12 |DIV                    |-         |-         |
|13 |MOD                    |-         |-         |
|14 |NEGATIVE               |-         |-         |
|15 |SHIFTLEFT              |-         |-         |
|16 |SHIFTRIGHT             |-         |-         |
|17 |BITWISE_OR             |-         |-         |
|18 |BITWISE_AND            |-         |-         |
|19 |BITWISE_XOR            |-         |-         |
|20 |BITWISE_COMPLEMENT     |-         |-         |
|21 |CONVERT                |-         |-         |
|22 |ASSIGN                 |-         |-         |
|23 |INC_BP                 |-         |-         |
|24 |DEC_BP                 |-         |-         |
|25 |INC_SP                 |-         |-         |
|26 |DEC_SP                 |-         |-         |
|27 |COMPARE                |-         |-         |
|28 |SETCC_EQ               |-         |-         |
|29 |SETCC_NEQ              |-         |-         |
|30 |SETCC_GT               |-         |-         |
|31 |SETCC_GTE              |-         |-         |
|32 |SETCC_LT               |-         |-         |
|33 |SETCC_LTE              |-         |-         |
|34 |JUMP                   |-         |-         |
|35 |JUMPIF_EQ              |-         |-         |
|36 |JUMPIF_NEQ             |-         |-         |
|37 |JUMPIF_GT              |-         |-         |
|38 |JUMPIF_GTE             |-         |-         |
|39 |JUMPIF_LT              |-         |-         |
|40 |JUMPIF_LTE             |-         |-         |
|41 |CALL                   |-         |-         |
|42 |RET                    |-         |-         |
|43 |IRET                   |-         |-         |
|44 |SET_INTERRUPT_HANDLER  |-         |-         |
|45 |DISABLE_INTERRUPTS     |-         |-         |
|46 |ENABLE_INTERRUPTS      |-         |-         |
|47 |REVERSE_STACK          |-         |-         |
|48 |SET_COUNTER            |-         |-         |
|49 |READ_COUNTER           |-         |-         |
|50 |SET_MEMORY_ARRAY       |-         |-         |
|51 |COPY_MEMORY_ARRAY      |-         |-         |


