using System;

namespace MethodPatch
{
    public class ClassToPatch
    {
        int i1 = 1, i2 = 1;

        public bool Write(string str)
        {
            return WriteInternal(str);
        }

        private bool WriteInternal(string str)
        {
            Console.WriteLine("Str: " + str);
            if (i1 == i2)
            {
                i2++;
                WriteInternal(str);
            }
            return true;
        }
    }
}
