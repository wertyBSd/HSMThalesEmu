using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands
{
    public interface IConsoleDataValidator
    {
        void ValidateConsoleMessage(string consoleMsg);
    }
}
