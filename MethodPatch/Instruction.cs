using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MethodPatch3
{
    public enum Instruction : byte
    {
        CALL = 0xE8,
        MOV = 0x48,
        CALL_ABSOLUTE = 0xFF,
        JMP = 0xEB,
        NOP = 0x90,
        RETN = 0xC3,
        PUSH = 0x50
    }
}