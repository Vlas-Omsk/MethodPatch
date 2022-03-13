using System;

namespace MethodPatch
{
    public sealed partial class MethodHook
    {
        private void Init32()
        {
            // WriteRegistersCommand32
            var registersCommandInstructionsSize = 11;
            // WriteCall32
            var callInstructionsSize = 5;

            _remapperSize =
                // WriteRegistersCommand32(..., WriteRegistersCommandType.Write)
                registersCommandInstructionsSize +
                // WriteCall32(...)
                callInstructionsSize +
                // WriteRegistersCommand32(..., WriteRegistersCommandType.Read)
                registersCommandInstructionsSize +
                // WriteCall32(...)
                callInstructionsSize +
                // WriteCall32(...)
                callInstructionsSize +
                // WriteInstruction(Instruction.RETN)
                1;
        }

        private void Hook32()
        {
            var writer = new LLWriter(_remapperPtr, _remapperSize);

            WriteRegistersCommand32(writer, WriteRegistersCommandType.Write);
            WriteCall32(writer, _beforeCallFunctionPtr);
            WriteRegistersCommand32(writer, WriteRegistersCommandType.Read);
            WriteCall32(writer, _targetMethod.FunctionPtr);
            WriteCall32(writer, _afterCallFunctionPtr);
            writer.WriteInstruction(Instruction.RETN);

#if DEBUG
            if (_targetMethod.IsDebuggerAttached)
            {
                writer.Set(_targetMethod.CallPtr + 1, IntPtr.Size);
                writer.WriteRelativePtr(_remapperPtr);
            }
            else
#endif
            {
                writer.Set(_targetMethod.CallPtr, IntPtr.Size);
                writer.WriteIntPtr(_remapperPtr);
            }
        }

        private void WriteCall32(LLWriter writer, IntPtr address)
        {
            writer
                .WriteInstruction(Instruction.CALL_REL16_32)
                .WriteRelativePtr(address);
        }

        private void WriteRegistersCommand32(LLWriter writer, WriteRegistersCommandType type)
        {
            writer
                .WriteByte(0xb8) // rax
                .WriteIntPtr(_registersBufferPtr);
            switch (type)
            {
                case WriteRegistersCommandType.Read:
                    writer.WriteByte(0x8B); // qword ptr ds:[rax],rcx
                    break;
                case WriteRegistersCommandType.Write:
                    writer.WriteByte(0x89); // rcx,qword ptr ds:[rax]
                    break;
                default:
                    throw new Exception();
            }
            writer
                .WriteByte(0x48)
                .WriteByte(0);
            switch (type)
            {
                case WriteRegistersCommandType.Read:
                    writer.WriteByte(0x8B); // qword ptr ds:[rax + 4],rdx)
                    break;
                case WriteRegistersCommandType.Write:
                    writer.WriteByte(0x89); // rdx,qword ptr ds:[rax + 4]
                    break;
                default:
                    throw new Exception();
            }
            writer
                .WriteByte(0x50)
                .WriteByte(4);
        }
    }
}
