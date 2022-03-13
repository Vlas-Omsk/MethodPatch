using System;

namespace MethodPatch
{
    public static class IntPtrExtension
    {
        public static string ToFormatString(this IntPtr intPtr)
        {
            if (IntPtr.Size == 4)
                return "0x" + intPtr.ToString("x8");
            else
                return "0x" + intPtr.ToString("x16");
        }
    }
}
