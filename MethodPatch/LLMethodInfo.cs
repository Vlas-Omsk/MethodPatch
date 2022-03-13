using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MethodPatch
{
    public unsafe sealed class LLMethodInfo
    {
        public IntPtr FunctionPtr { get; private set; }
        public IntPtr CallPtr { get; private set; }
        public MethodInfo MethodInfo { get; private set; }
        public bool IsDebuggerAttached { get; private set; } = false;

        public LLMethodInfo(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
            Init();
        }

        private void Init()
        {
            RuntimeHelpers.PrepareMethod(MethodInfo.MethodHandle);
            FunctionPtr = MethodInfo.MethodHandle.GetFunctionPointer();

#if DEBUG
            if (*(byte*)FunctionPtr == 0xe9)
                IsDebuggerAttached = true;
#endif

#if DEBUG
            if (IsDebuggerAttached)
            {
                CallPtr = FunctionPtr;
                FunctionPtr = AssemblerHelper.GetAbsolutePtr(CallPtr + 1, new IntPtr(*(int*)(CallPtr + 1)), 4);
            }
            else
#endif
            {
                CallPtr = MethodInfo.MethodHandle.Value + 8;
            }
        }
    }
}
