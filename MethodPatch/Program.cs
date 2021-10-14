using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MethodPatch3
{
    class Program
    {
        // Tests .NET Framework 4.7.2
        //  X32
        //   Debug x32 (with debugger)   | + |
        //   Debug x32                   | + |
        //   Release x32 (with debugger) | + |
        //   Release x32                 | + |
        //  X64
        //   Debug x64 (with debugger)   | - | Little space
        //   Debug x64                   | ~ | AccessViolationException at the end
        //   Release x64 (with debugger) | - | AccessViolationException
        //   Release x64                 | + |

        public unsafe static void Main(string[] args)
        {
            var writeInternalMethodInfo = typeof(ClassToPatch).GetMethod("WriteInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            var methodHook = new MethodHook(writeInternalMethodInfo);
            methodHook.Hook();
            methodHook.BeforeCall += MethodHook_BeforeCall;
            methodHook.AfterCall += MethodHook_AfterCall;

            // END
            Console.ReadLine();
            new ClassToPatch().Write("Hello world!");
            Console.ReadLine();
        }

        private static void MethodHook_AfterCall(object sender, EventArgs e)
        {
            Console.WriteLine("Hello world! (Hook: AfterCall)");
        }

        private static void MethodHook_BeforeCall(object sender, EventArgs e)
        {
            Console.WriteLine("Hello world! (Hook: BeforeCall)");
        }
    }
}
