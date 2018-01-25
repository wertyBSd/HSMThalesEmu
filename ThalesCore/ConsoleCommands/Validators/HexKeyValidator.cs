using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class HexKeyValidator : IConsoleDataValidator
    {
        public void ValidateConsoleMessage(string consoleMsg)
        {
            

            consoleMsg = consoleMsg.ToUpper();

            if ((Utility.IsHexString(consoleMsg) == false) ||
                ( (consoleMsg.Length != 16) && (consoleMsg.Length != 32) && (consoleMsg.Length != 48) ))
                throw new Exceptions.XInvalidKey("INVALID");
        }
    }
}
