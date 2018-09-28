using HostCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Message;
using ThalesCore.Message.XML;

namespace ThalesCore.HostCommands.BuildIn
{
	public class HSMStatus_NO: AHostCommand
	{
		private string _modeFlag = string.Empty;
		public HSMStatus_NO()
		{
			ReadXMLDefinitions();
		}

		public override void AcceptMessage(Message.Message msg)
		{
			string xMLParseResult = XMLParseResult;
			MessageParser.Parse(msg, XMLMessageFields, ref kvp, out xMLParseResult);
			if (xMLParseResult == ErrorCodes.ER_00_NO_ERROR)
			{
				_modeFlag = kvp.Item("Mode Flag");
			}
		}

		public override MessageResponse ConstructResponse()
		{
			MessageResponse mr = new MessageResponse();
			mr.AddElement(ErrorCodes.ER_00_NO_ERROR);

			if (_modeFlag == "00")
			{
				mr.AddElement("3");
				mr.AddElement("1");
				mr.AddElement(Convert.ToInt32(Resources.GetResource(Resources.MAX_CONS)).ToString());
				mr.AddElement(Convert.ToString(Resources.GetResource(Resources.FIRMWARE_NUMBER)));
				mr.AddElement("0");
				mr.AddElement(Convert.ToString(Resources.GetResource(Resources.DSP_FIRMWARE_NUMBER)));
			}
			return mr;
		}
	}
}
