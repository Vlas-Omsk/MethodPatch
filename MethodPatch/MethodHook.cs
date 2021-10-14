using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace MethodPatch3
{
    public unsafe class MethodHook
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void HookCallbackDelegate();

        const uint PAGE_EXECUTE_READWRITE = 0x40;

        public MethodInfo MethodInfo { get; }

        private HookCallbackDelegate _hookBeforeCallDelegate, _hookAfterCallDelegate;
        private IntPtr _targetCallPtr, _targetFunctionPtr;
        private IntPtr _hookBeforeCallFunctionPtr, _hookAfterCallFunctionPtr;
        private IntPtr _remapperFunctionPtr, _saveRegistersPtr;
        private int _callInstructionsSize, _rspCommandInstructionsSize;
        private int _remapperSize, _backupRegistersInstructionsSize, _backupRegistersMemorySize;
#if DEBUG
        private bool _isDebuggerAttached;
#endif

        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        public MethodHook(MethodInfo methodInfo)
        {
            RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);

            MethodInfo = methodInfo;
            _hookBeforeCallDelegate = new HookCallbackDelegate(OnBeforeCall);
            _hookAfterCallDelegate = new HookCallbackDelegate(OnAfterCall);
            _targetFunctionPtr = methodInfo.MethodHandle.GetFunctionPointer();
            _hookBeforeCallFunctionPtr = Marshal.GetFunctionPointerForDelegate(_hookBeforeCallDelegate);
            _hookAfterCallFunctionPtr = Marshal.GetFunctionPointerForDelegate(_hookAfterCallDelegate);

#if DEBUG
            if (*(byte*)_targetFunctionPtr == 0xe9)
                _isDebuggerAttached = true;
#endif

            Console.WriteLine("HookBeforeCallFunctionPtr: 0x" + _hookBeforeCallFunctionPtr.ToFormatString());
            Console.WriteLine("HookAfterCallFunctionPtr: 0x" + _hookAfterCallFunctionPtr.ToFormatString());

            if (IntPtr.Size == 4)
                InitX32();
            else
                InitX64();

            _saveRegistersPtr = Marshal.AllocHGlobal(_backupRegistersMemorySize);
            VirtualProtect(_saveRegistersPtr, new UIntPtr((uint)_backupRegistersMemorySize), PAGE_EXECUTE_READWRITE, out _);
            _remapperFunctionPtr = Marshal.AllocHGlobal(_remapperSize);
            VirtualProtect(_remapperFunctionPtr, new UIntPtr((uint)_remapperSize), PAGE_EXECUTE_READWRITE, out _);

            Console.WriteLine("TargetCallPtr: 0x" + _targetCallPtr.ToFormatString());
            Console.WriteLine("TargetFunctionPtr: 0x" + _targetFunctionPtr.ToFormatString());
            Console.WriteLine("SaveRegistersPtr: 0x" + _saveRegistersPtr.ToFormatString());
            Console.WriteLine("RemapperFunctionPtr: 0x" + _remapperFunctionPtr.ToFormatString());
        }

        public void Hook()
        {
            if (IntPtr.Size == 4)
                HookX32();
            else
                HookX64();
        }

#region X64
        private void InitX64()
        {
            Console.WriteLine("Platform x64");

#if DEBUG
            if (_isDebuggerAttached)
            {
                _targetCallPtr = _targetFunctionPtr;
                _targetFunctionPtr = GetAbsolutePtr(_targetCallPtr + 1, new IntPtr(*(int*)(_targetCallPtr + 1)), 4);
            }
            else
#endif
            {
                _targetCallPtr = MethodInfo.MethodHandle.Value + 8;
            }


            _rspCommandInstructionsSize = 4;
            _backupRegistersInstructionsSize = 18;
            _backupRegistersMemorySize = 8 * 2;
            _callInstructionsSize = 12;
            _remapperSize =
                _rspCommandInstructionsSize +
                _backupRegistersInstructionsSize +
                _callInstructionsSize +
                _backupRegistersInstructionsSize +
                _callInstructionsSize +
                _callInstructionsSize +
                _rspCommandInstructionsSize + 1;
        }

        private void HookX64()
        {
            var position = _remapperFunctionPtr;
            WriteRspCommand(position, 8);
            position += _rspCommandInstructionsSize;
            WriteX64BackupRegisters(position);
            position += _backupRegistersInstructionsSize;
            WriteX64Call(position, _hookBeforeCallFunctionPtr);
            position += _callInstructionsSize;
            WriteX64BackupRegisters(position, true);
            position += _backupRegistersInstructionsSize;
            WriteX64Call(position, _targetFunctionPtr);
            position += _callInstructionsSize;
            WriteX64Call(position, _hookAfterCallFunctionPtr);
            position += _callInstructionsSize;
            WriteRspCommand(position, 8, true);
            position += _rspCommandInstructionsSize;
            *(byte*)position = (byte)Instruction.RETN;

#if DEBUG
            if (_isDebuggerAttached)
            {
                throw new NotImplementedException("Little space");
            }
            else
#endif
            {
                position = _targetCallPtr;
                *(long*)position = _remapperFunctionPtr.ToInt64();
            }
        }

        private void WriteX64Call(IntPtr ptr, IntPtr address)
        {
            *(byte*)ptr = (byte)Instruction.MOV;
            *(byte*)(ptr + 1) = 0xb8; // rax
            *(long*)(ptr + 2) = address.ToInt64();
            *(byte*)(ptr + 10) = (byte)Instruction.CALL_ABSOLUTE;
            *(byte*)(ptr + 11) = 0xd0; // rax
        }

        private void WriteRspCommand(IntPtr ptr, byte amount, bool add = false)
        {
            *(byte*)ptr = 0x48;
            *(byte*)(ptr + 1) = 0x83;
            if (add)
                *(byte*)(ptr + 2) = 0xc4; // add rsp,$amount
            else
                *(byte*)(ptr + 2) = 0xec; // sub rsp,$amount
            *(byte*)(ptr + 3) = amount;
        }

        private void WriteX64BackupRegisters(IntPtr ptr, bool load = false)
        {
            *(byte*)ptr = (byte)Instruction.MOV;
            *(byte*)(ptr + 1) = 0xb8; // rax
            *(long*)(ptr + 2) = _saveRegistersPtr.ToInt64();
            *(byte*)(ptr + 10) = (byte)Instruction.MOV;
            if (load)
                *(byte*)(ptr + 11) = 0x8B; // qword ptr ds:[rax],rcx
            else
                *(byte*)(ptr + 11) = 0x89; // rcx,qword ptr ds:[rax]
            *(byte*)(ptr + 12) = 0x48;
            *(byte*)(ptr + 13) = 0;
            *(byte*)(ptr + 14) = (byte)Instruction.MOV;
            if (load)
                *(byte*)(ptr + 15) = 0x8B; // qword ptr ds:[rax + 8],rdx
            else
                *(byte*)(ptr + 15) = 0x89; // rdx,qword ptr ds:[rax + 8]
            *(byte*)(ptr + 16) = 0x50;
            *(byte*)(ptr + 17) = 8;
        }
#endregion

#region X32
        private void InitX32()
        {
            Console.WriteLine("Platform x32");

#if DEBUG
            if (_isDebuggerAttached)
            {
                _targetCallPtr = _targetFunctionPtr;
                _targetFunctionPtr = GetAbsolutePtr(_targetCallPtr + 1, new IntPtr(*(int*)(_targetCallPtr + 1)), 4);
            }
            else
#endif
            {
                _targetCallPtr = MethodInfo.MethodHandle.Value + 8;
            }

            _backupRegistersInstructionsSize = 11;
            _backupRegistersMemorySize = 4 * 2;
            _callInstructionsSize = 5;
            _remapperSize = _backupRegistersInstructionsSize + _callInstructionsSize + _backupRegistersInstructionsSize + _callInstructionsSize + _callInstructionsSize + 1;
        }

        private void HookX32()
        {
            var position = _remapperFunctionPtr;
            WriteX32BackupRegisters(position);
            position += _backupRegistersInstructionsSize;
            WriteX32Call(position, _hookBeforeCallFunctionPtr);
            position += _callInstructionsSize;
            WriteX32BackupRegisters(position, true);
            position += _backupRegistersInstructionsSize;
            WriteX32Call(position, _targetFunctionPtr);
            position += _callInstructionsSize;
            WriteX32Call(position, _hookAfterCallFunctionPtr);
            position += _callInstructionsSize;
            *(byte*)position = (byte)Instruction.RETN;

#if DEBUG
            if (_isDebuggerAttached)
            {
                position = _targetCallPtr + 1;
                *(int*)position = GetRelativePtr(position, _remapperFunctionPtr, 4).ToInt32();
            }
            else
#endif
            {
                position = _targetCallPtr;
                *(int*)position = _remapperFunctionPtr.ToInt32();
            }
        }

        private void WriteX32Call(IntPtr ptr, IntPtr address)
        {
            *(byte*)ptr = (byte)Instruction.CALL;
            *(int*)(ptr + 1) = GetRelativePtr(ptr + 1, address, 4).ToInt32();
        }

        private void WriteX32BackupRegisters(IntPtr ptr, bool load = false)
        {
            *(byte*)ptr = 0xb8; // rax
            *(long*)(ptr + 1) = _saveRegistersPtr.ToInt32();
            if (load)
                *(byte*)(ptr + 5) = 0x8B; // qword ptr ds:[rax],rcx
            else
                *(byte*)(ptr + 5) = 0x89; // rcx,qword ptr ds:[rax]
            *(byte*)(ptr + 6) = 0x48;
            *(byte*)(ptr + 7) = 0;
            if (load)
                *(byte*)(ptr + 8) = 0x8B; // qword ptr ds:[rax + 4],rdx
            else
                *(byte*)(ptr + 8) = 0x89; // rdx,qword ptr ds:[rax + 4]
            *(byte*)(ptr + 9) = 0x50;
            *(byte*)(ptr + 10) = 4;
        }
#endregion

        private static IntPtr GetAbsolutePtr(IntPtr origin, IntPtr address, int offset)
        {
            return new IntPtr(origin.ToInt64() + offset + address.ToInt64());
        }

        private static IntPtr GetRelativePtr(IntPtr origin, IntPtr address, int offset)
        {
            return new IntPtr(address.ToInt64() - origin.ToInt64() - offset);
        }

        private void OnBeforeCall()
        {
            BeforeCall?.Invoke(this, EventArgs.Empty);
        }

        private void OnAfterCall()
        {
            AfterCall?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<EventArgs> BeforeCall, AfterCall;
    }
}
