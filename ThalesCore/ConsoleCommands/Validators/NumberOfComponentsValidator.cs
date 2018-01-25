using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class NumberOfComponentsValidator : IConsoleDataValidator
    {
        public void ValidateConsoleMessage(string consoleMsg)
        {
            try
            {
                int nbr = Convert.ToInt32(consoleMsg);
                if ((nbr < 2) || (nbr > 9))
                    throw new Exceptions.XInvalidNumberOfComponents("INVALID NUMBER OF COMPONENTS");
            }
            catch (Exception ex)
            {
                throw new Exceptions.XInvalidNumberOfComponents("INVALID NUMBER OF COMPONENTS");
            }
        }
    }
}
