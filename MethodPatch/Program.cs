using System;
using System.Reflection;

namespace MethodPatch
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
        //   Debug x64 (with debugger)   | - | Not enough space
        //   Debug x64                   | ~ | AccessViolationException after execution
        //   Release x64 (with debugger) | - | AccessViolationException
        //   Release x64                 | + |

        unsafe static void Main(string[] args)
        {
            var writeInternalMethodInfo = typeof(ClassToPatch).GetMethod("WriteInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            var methodHook = new MethodHook(writeInternalMethodInfo);
            methodHook.AfterCall = MethodHook_AfterCall;
            methodHook.BeforeCall = MethodHook_BeforeCall;
            methodHook.Hook();

            // END
            Console.ReadLine();
            new ClassToPatch().Write("Hello world!");
            Console.ReadLine();
        }

        private static void MethodHook_AfterCall()
        {
            Console.WriteLine("Hello world! (Hook: AfterCall)");
        }

        private static void MethodHook_BeforeCall()
        {
            Console.WriteLine("Hello world! (Hook: BeforeCall)");
        }
    }
}
