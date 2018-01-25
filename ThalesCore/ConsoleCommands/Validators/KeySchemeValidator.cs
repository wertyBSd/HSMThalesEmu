using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class KeySchemeValidator: IConsoleDataValidator
    {
        bool ignoreEmpty;

        public KeySchemeValidator()
        {
            ignoreEmpty = false;
        }

        public KeySchemeValidator(bool ignoreEmpty)
        {
            this.ignoreEmpty = ignoreEmpty;
        }

        public void ValidateConsoleMessage(string consoleMsg)
        {
            if ((ignoreEmpty) && (consoleMsg == ""))
                return;

            consoleMsg = consoleMsg.ToUpper();

            if ((consoleMsg != "0") && (consoleMsg != "U") && (consoleMsg != "T") && (consoleMsg != "X") && (consoleMsg != "Y"))
                throw new Exceptions.XInvalidKeyScheme("Invalid key scheme, must be 0, U, T, X or Y");
        }
    }
}
