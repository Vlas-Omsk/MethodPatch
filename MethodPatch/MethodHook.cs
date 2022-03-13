using MethodPatch.WinApi;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MethodPatch
{
    public delegate void HookCallback();

    public sealed partial class MethodHook
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void UnmanagedHookCallback();

        private enum WriteRegistersCommandType
        {
            Read,
            Write
        }

        private enum WriteRspCommandType
        {
            Add,
            Sub
        }

        public HookCallback BeforeCall { get; set; }
        public HookCallback AfterCall { get; set; }

        private LLMethodInfo _targetMethod;
        private UnmanagedHookCallback _beforeCallUnmanaged;
        private UnmanagedHookCallback _afterCallUnmanaged;
        private IntPtr _beforeCallFunctionPtr;
        private IntPtr _afterCallFunctionPtr;

        private int _remapperSize;
        private IntPtr _remapperPtr;
        private int _registersBufferSize;
        private IntPtr _registersBufferPtr;

        public MethodHook(MethodInfo methodInfo)
        {
            _targetMethod = new LLMethodInfo(methodInfo);
            _beforeCallUnmanaged = OnBeforeCall;
            _afterCallUnmanaged = OnAfterCall;
            _beforeCallFunctionPtr = Marshal.GetFunctionPointerForDelegate(_beforeCallUnmanaged);
            _afterCallFunctionPtr = Marshal.GetFunctionPointerForDelegate(_afterCallUnmanaged);

            Init();

            Console.WriteLine($"{nameof(_targetMethod.IsDebuggerAttached)}: {_targetMethod.IsDebuggerAttached}");
            Console.WriteLine($"{nameof(_targetMethod.FunctionPtr)}: {_targetMethod.FunctionPtr.ToFormatString()}");
            Console.WriteLine($"{nameof(_targetMethod.CallPtr)}: {_targetMethod.CallPtr.ToFormatString()}");
            Console.WriteLine($"{nameof(_beforeCallFunctionPtr)}: {_beforeCallFunctionPtr.ToFormatString()}");
            Console.WriteLine($"{nameof(_afterCallFunctionPtr)}: {_afterCallFunctionPtr.ToFormatString()}");
            Console.WriteLine($"{nameof(_remapperSize)}: {_remapperSize}");
            Console.WriteLine($"{nameof(_remapperPtr)}: {_remapperPtr.ToFormatString()}");
            Console.WriteLine($"{nameof(_registersBufferSize)}: {_registersBufferSize}");
            Console.WriteLine($"{nameof(_registersBufferPtr)}: {_registersBufferPtr.ToFormatString()}");
        }

        private void Init()
        {
            _registersBufferSize = IntPtr.Size * 2;

            if (IntPtr.Size == 4)
                Init32();
            else
                Init64();

            _registersBufferPtr = Alloc(_registersBufferSize);
            _remapperPtr = Alloc(_remapperSize);
        }

        public void Hook()
        {
            if (IntPtr.Size == 4)
                Hook32();
            else
                Hook64();
        }

        private IntPtr Alloc(int size)
        {
            var ptr = Marshal.AllocHGlobal(size);
            Kernel32.VirtualProtect(ptr, new UIntPtr((uint)size), Constants.PAGE_EXECUTE_READWRITE, out _);
            return ptr;
        }

        private void OnBeforeCall()
        {
            BeforeCall?.Invoke();
        }

        private void OnAfterCall()
        {
            AfterCall?.Invoke();
        }
    }
}
