using System;

namespace MethodPatch
{
    internal static class AssemblerHelper
    {
        public static IntPtr GetAbsolutePtr(IntPtr origin, IntPtr address, int offset)
        {
            return new IntPtr(origin.ToInt64() + offset + address.ToInt64());
        }

        public static IntPtr GetRelativePtr(IntPtr origin, IntPtr address, int offset)
        {
            return new IntPtr(address.ToInt64() - origin.ToInt64() - offset);
        }
    }
}
