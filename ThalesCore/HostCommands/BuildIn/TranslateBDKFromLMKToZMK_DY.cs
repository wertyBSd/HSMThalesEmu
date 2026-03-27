using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("DY", "DZ", "", "Translates a BDK from LMK to encryption under ZMK.")]
    public class TranslateBDKFromLMKToZMK_DY : AHostCommand
    {
        public TranslateBDKFromLMKToZMK_DY()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            // Basic sanity checks: reject overly-long or non-printable payloads
            int remaining = msg.CharsLeft();
            Log.Logger.MinorDebug($"DY AcceptMessage: chars left={remaining}");
            if (remaining > 2048)
            {
                XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
                return;
            }

            if (msg.CurrentIndex < msg.MessageData.Length)
            {
                string rem = msg.MessageData.Substring(msg.CurrentIndex).TrimEnd('\r', '\n');
                Log.Logger.MinorDebug($"DY AcceptMessage: remainder='{rem}'");
                foreach (char c in rem)
                {
                    if (c < 0x20 || c > 0x7E)
                    {
                        Log.Logger.MinorDebug($"DY AcceptMessage: non-printable char detected (0x{((int)c):X2})");
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
                Log.Logger.MinorDebug($"DY AcceptMessage: exception during parse: {ex.Message}");
                XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
                return;
            }
            XMLParseResult = string.IsNullOrEmpty(ret) ? ErrorCodes.ER_00_NO_ERROR : ret;
            Log.Logger.MinorDebug($"DY AcceptMessage: parse result={XMLParseResult}");
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();
            if (XMLParseResult != ErrorCodes.ER_00_NO_ERROR)
            {
                mr.AddElement(XMLParseResult);
                return mr;
            }
            // Actual cryptographic translation not implemented; return success code for now.
            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            return mr;
        }
    }
}
