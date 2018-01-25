using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands
{
    [AttributeUsage(AttributeTargets.Class)] 
    public class ThalesConsoleCommandCode : Attribute
    {
        public string ConsoleCommandCode;

        public string Description;

        public ThalesConsoleCommandCode(string consoleCode, string description)
        {
            this.ConsoleCommandCode = consoleCode;
            this.Description = description;
        }
    }
}
