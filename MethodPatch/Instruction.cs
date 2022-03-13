using System;

namespace MethodPatch
{
    public enum Instruction : byte
    {
        /// <summary>
        /// Call Procedure
        /// </summary>
        CALL_REL16_32 = 0xE8,
        /// <summary>
        /// ???
        /// </summary>
        MOV = 0x48,
        /// <summary>
        /// Call Procedure
        /// </summary>
        CALL_ABSOLUTE = 0xFF,
        /// <summary>
        /// Jump
        /// </summary>
        JMP_REL8 = 0xEB,
        /// <summary>
        /// No Operation
        /// </summary>
        NOP = 0x90,
        /// <summary>
        /// Return from procedure
        /// </summary>
        RETN = 0xC3,
        /// <summary>
        /// Push Word, Doubleword or Quadword Onto the Stack
        /// </summary>
        PUSH_R16_32 = 0x50
    }
}