using HostCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
	[ThalesCommandCode("LG", "LH", "", "Sets an HSM response delay")]
	public class SetHSMDelay_LG: AHostCommand
	{
		private string _delay = string.Empty;
		public SetHSMDelay_LG()
		{
			ReadXMLDefinitions();
		}

		public override void AcceptMessage(Message.Message msg)
		{
			string ret = string.Empty;
			ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
			if (ret == ErrorCodes.ER_00_NO_ERROR)
			{
				_delay = kvp.Item("Delay");
				// parse and store globally so services can honor the configured delay
				if (int.TryParse(_delay, out var ms) && ms >= 0)
				{
					ThalesCore.HSMSettings.ResponseDelayMs = ms;
				}
				else
				{
					ret = ErrorCodes.ER_15_INVALID_INPUT_DATA;
				}
			}
		}

		public override MessageResponse ConstructResponse()
		{
			MessageResponse mr = new MessageResponse();
			Log.Logger.MajorInfo("HSM");
			mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
			return mr;
		}
	}
}
