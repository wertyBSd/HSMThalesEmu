using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("B2", "B3", "", "Echo received data back to the user")]
    public class EchoTest_B2 : AHostCommand
    {
        

        public EchoTest_B2()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            string ret = string.Empty;
            ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            XMLParseResult = ret;
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();
            if (!String.IsNullOrEmpty(XMLParseResult) && XMLParseResult != ErrorCodes.ER_00_NO_ERROR)
            {
                mr.AddElement(XMLParseResult);
            }
            else
            {
                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            }
            return mr;
        }
    }
}
