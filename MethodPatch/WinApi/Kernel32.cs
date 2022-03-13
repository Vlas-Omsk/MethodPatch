using System;
using System.Runtime.InteropServices;

namespace MethodPatch.WinApi
{
    internal static class Kernel32
    {
        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
    }
}
