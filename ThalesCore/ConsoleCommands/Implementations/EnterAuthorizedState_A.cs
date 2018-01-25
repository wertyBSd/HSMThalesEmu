using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Log;

namespace ThalesCore.ConsoleCommands.Validators
{
    [ThalesConsoleCommandCode("A", "Enters the authorized state")]
    public class EnterAuthorizedState_A : AConsoleCommand
    {
        public override void InitializeStack()
        { }

        public override string ProcessMessage()
        {
            Logger.MajorInfo("Entering authorized state");
            Resources.UpdateResource(Resources.AUTHORIZED_STATE, true);
            return "AUTHORIZED";
        }
    }
}
