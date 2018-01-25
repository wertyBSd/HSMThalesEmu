using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class KeyLengthValidator : IConsoleDataValidator
    {
        public void ValidateConsoleMessage(string consoleMsg)
        {
            if ((consoleMsg != "1") && (consoleMsg != "2") && (consoleMsg != "3"))
                throw new Exceptions.XInvalidKeyLength("Invalid key length (must be 1,2 or 3)");
        }
    }
}
