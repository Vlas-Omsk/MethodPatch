using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MethodPatch3
{
    public static class IntPtrEx
    {
        public static string ToFormatString(this IntPtr intPtr)
        {
            if (IntPtr.Size == 4)
                return intPtr.ToString("x8");
            else
                return intPtr.ToString("x16");
        }
    }
}
