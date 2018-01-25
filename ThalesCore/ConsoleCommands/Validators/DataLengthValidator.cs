using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class DataLengthValidator : IConsoleDataValidator
    {
        public void ValidateConsoleMessage(string consoleMsg)
        {
            consoleMsg = consoleMsg.ToUpper();

            if ((consoleMsg != "S") && (consoleMsg != "D") && (consoleMsg != "T"))
                throw new Exceptions.XInvalidData("INVALID LENGTH");
        }
    }
}
