using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("G0", "G1", "", "Translates a PIN block from DUKPT to ZPK encryption (3DES).")]
    public class TranslatePINFromDUKPTToZPK3DES_G0 : AHostCommand
    {
        public TranslatePINFromDUKPTToZPK3DES_G0()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            // Basic sanity checks: reject overly-long or non-printable payloads
            int remaining = msg.CharsLeft();
            Log.Logger.MinorDebug($"G0 AcceptMessage: chars left={remaining}");
            if (remaining > 2048)
            {
                XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
                return;
            }

            if (msg.CurrentIndex < msg.MessageData.Length)
            {
                string rem = msg.MessageData.Substring(msg.CurrentIndex).TrimEnd('\r', '\n');
                Log.Logger.MinorDebug($"G0 AcceptMessage: remainder='{rem}'");
                foreach (char c in rem)
                {
                    if (c < 0x20 || c > 0x7E)
                    {
                        Log.Logger.MinorDebug($"G0 AcceptMessage: non-printable char detected (0x{((int)c):X2})");
                        XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
                        return;
                    }
                }
            }

            string ret = string.Empty;
            try
            {
                ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            }
            catch (Exception ex)
            {
                Log.Logger.MinorDebug($"G0 AcceptMessage: exception during parse: {ex.Message}");
                XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
                return;
            }
            XMLParseResult = string.IsNullOrEmpty(ret) ? ErrorCodes.ER_00_NO_ERROR : ret;
            Log.Logger.MinorDebug($"G0 AcceptMessage: parse result={XMLParseResult}");
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();
            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            return mr;
        }
    }
}
