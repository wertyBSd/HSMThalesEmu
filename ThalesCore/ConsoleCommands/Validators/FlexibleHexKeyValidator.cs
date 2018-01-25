using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class FlexibleHexKeyValidator : IConsoleDataValidator
    {
        bool ignoreEmptyKey;

        public FlexibleHexKeyValidator()
        {
            ignoreEmptyKey = false;
        }

        public FlexibleHexKeyValidator(bool ignoreEmptyKey)
        {
            this.ignoreEmptyKey = ignoreEmptyKey;
        }

        public void ValidateConsoleMessage(string consoleMsg)
        {
            consoleMsg = consoleMsg.ToUpper();

            if ((ignoreEmptyKey) && (consoleMsg == ""))
                return;

            if ((Utility.IsHexString(consoleMsg) == false) || (consoleMsg.Length != 8))
            {
                try
                {
                    HexKey key = new HexKey(consoleMsg);
                }
                catch (Exception ex)
                {
                    throw new Exceptions.XInvalidKey("INVALID KEY");
                }
            }
        }
    }
}
