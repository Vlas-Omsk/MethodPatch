using System;

namespace MethodPatch
{
    public sealed partial class MethodHook
    {
        private void Init64()
        {
            // WriteRspCommand64
            var rspCommandInstructionsSize = 4;
            // WriteRegistersCommand64
            var registersCommandInstructionsSize = 18;
            // WriteCall64
            var callInstructionsSize = 12;

            _remapperSize =
                // WriteRspCommand64(..., WriteRspCommandType.Sub)
                rspCommandInstructionsSize +
                // WriteRegistersCommand64(..., WriteRegistersCommandType.Write)
                registersCommandInstructionsSize +
                // WriteCall64(...)
                callInstructionsSize +
                // WriteRegistersCommand64(..., WriteRegistersCommandType.Read)
                registersCommandInstructionsSize +
                // WriteCall64(...)
                callInstructionsSize +
                // WriteCall64(...)
                callInstructionsSize +
                // WriteRspCommand64(..., WriteRspCommandType.Add)
                rspCommandInstructionsSize +
                // WriteInstruction(Instruction.RETN)
                1;
        }

        private void Hook64()
        {
            var writer = new LLWriter(_remapperPtr, _remapperSize);

            WriteRspCommand64(writer, 8, WriteRspCommandType.Sub);
            WriteRegistersCommand64(writer, WriteRegistersCommandType.Write);
            WriteCall64(writer, _beforeCallFunctionPtr);
            WriteRegistersCommand64(writer, WriteRegistersCommandType.Read);
            WriteCall64(writer, _targetMethod.FunctionPtr);
            WriteCall64(writer, _afterCallFunctionPtr);
            WriteRspCommand64(writer, 8, WriteRspCommandType.Add);
            writer.WriteInstruction(Instruction.RETN);

#if DEBUG
            if (_targetMethod.IsDebuggerAttached)
            {
                // The debugger creates a jump to the address of the function with a relative,
                // 32-bit offset that cannot be replaced by an absolute value due to lack of space.
                throw new NotImplementedException("Not enough space");
            }
            else
#endif
            {
                writer.Set(_targetMethod.CallPtr, IntPtr.Size);
                writer.WriteIntPtr(_remapperPtr);
            }
        }

        private void WriteRspCommand64(LLWriter writer, byte amount, WriteRspCommandType type)
        {
            writer
                .WriteByte(0x48)
                .WriteByte(0x83);
            switch (type)
            {
                case WriteRspCommandType.Add:
                    writer.WriteByte(0xc4); // add rsp,$amount
                    break;
                case WriteRspCommandType.Sub:
                    writer.WriteByte(0xec); // sub rsp,$amount
                    break;
            }
            writer.WriteByte(amount);
        }

        private void WriteRegistersCommand64(LLWriter writer, WriteRegistersCommandType type)
        {
            writer
                .WriteInstruction(Instruction.MOV)
                .WriteByte(0xb8) // rax
                .WriteIntPtr(_registersBufferPtr)
                .WriteInstruction(Instruction.MOV);
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
                .WriteByte(0)
                .WriteInstruction(Instruction.MOV);
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
                .WriteByte(8);
        }

        private void WriteCall64(LLWriter writer, IntPtr address)
        {
            writer
                .WriteInstruction(Instruction.MOV)
                .WriteByte(0xb8) // rax
                .WriteIntPtr(address)
                .WriteInstruction(Instruction.CALL_ABSOLUTE)
                .WriteByte(0xd0); // rax
        }
    }
}
