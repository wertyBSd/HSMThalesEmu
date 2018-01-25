using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Log;

namespace ThalesCore.ConsoleCommands.Validators
{
    public class CancelAuthorizedState_C : AConsoleCommand
    {
        public override void InitializeStack()
        { }

        public override string ProcessMessage()
        {
            Logger.MajorInfo("Canceling authorized state");
            Resources.UpdateResource(Resources.AUTHORIZED_STATE, true);
            return "NOT AUTHORIZED";
        }
    }
}
