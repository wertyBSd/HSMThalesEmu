using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class ComponentTypeValidator : IConsoleDataValidator
    {
        public void ValidateConsoleMessage(string consoleMsg)
        {
            if (consoleMsg == "S")
                throw new Exceptions.XInvalidComponentType("INPUT FROM SMART CARDS NOT SUPPORTED");
            else if((consoleMsg != "X") && (consoleMsg != "H") && (consoleMsg != "E"))
                throw new Exceptions.XInvalidComponentType("INVALID COMPONENT TYPE");
        }
    }
}
