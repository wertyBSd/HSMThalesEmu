using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class AuthorizedStateValidator : IConsoleDataValidator
    {
        public void ValidateConsoleMessage(string consoleMsg)
        {
            if(!Convert.ToBoolean(Resources.GetResource(Resources.AUTHORIZED_STATE)))
                throw new Exceptions.XNeedsAuthorizedState("NOT AUTHORIZED");
        }
    }
}
