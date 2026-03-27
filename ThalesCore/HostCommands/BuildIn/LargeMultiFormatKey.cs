using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    /// <summary>
    /// Helper class used to load the XML definition fragment for a large multi-format key.
    /// This is not a runnable host command; it exists so the XML parser can
    /// include the corresponding XML field definition where needed.
    /// Do not assign a ThalesCommandCode attribute to this class.
    /// </summary>
    public class LargeMultiFormatKey : AHostCommand
    {
        public LargeMultiFormatKey()
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
            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            return mr;
        }
    }
}
