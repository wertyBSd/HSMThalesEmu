using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace TestCoreConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string s = Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity);
            Console.WriteLine(s);
            Console.Read();
        }
    }
}
