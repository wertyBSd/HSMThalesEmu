using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class KeyTypeValidator : IConsoleDataValidator
    {
        public void ValidateConsoleMessage(string consoleMsg)
        {
            LMKPairs.LMKPair tmp;
            string tempVar = "";
            KeyTypeTable.ParseKeyTypeCode(consoleMsg,out tmp, out tempVar);
        }
    }
}
