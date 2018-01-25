using HostCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("RA", "RB", "", "Cancel the authorized state")]
    public class CancelAuthState_RA: AHostCommand
    {
        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();
            if (!IsInAuthorizedState())
                Log.Logger.MajorInfo("Exiting from AUTHORIZED state");
            else
                Log.Logger.MajorInfo("Already out of the AUTHORIZED state");
            Resources.UpdateResource(Resources.AUTHORIZED_STATE, false);
            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            return mr;
        }
    }
}
