using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("BY", "BZ", "", "Translates a ZMK from encryption under a ZMK to encryption under LMK.")]
    public class TranslateZMKFromZMKToLMK_BY : AHostCommand
    {
        public TranslateZMKFromZMKToLMK_BY()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            if (msg == null)
            {
                XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
                return;
            }

            if (!string.IsNullOrEmpty(msg.RemainingData) && msg.RemainingData.Length > 2048)
            {
                XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
                return;
            }

            foreach (char c in msg.RemainingData ?? string.Empty)
            {
                if (c < 0x20 || c > 0x7E)
                {
                    XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
                    return;
                }
            }

            try
            {
                string ret = string.Empty;
                ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
                XMLParseResult = ret;
            }
            catch (Exception)
            {
                XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
            }
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();
            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            return mr;
        }
    }
}
