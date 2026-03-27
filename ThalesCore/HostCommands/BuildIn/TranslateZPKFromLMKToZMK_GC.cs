using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("GC", "GD", "", "Translates a ZPK from encryption under the LMK to encryption under a ZMK.")]
    public class TranslateZPKFromLMKToZMK_GC : AHostCommand
    {
        public TranslateZPKFromLMKToZMK_GC()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            // Basic sanity checks: reject overly-long or non-printable payloads
            int remaining = msg.CharsLeft();
            Log.Logger.MinorDebug($"GC AcceptMessage: chars left={remaining}");
            // reject if excessively long (defensive threshold)
            if (remaining > 2048)
            {
                XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
                return;
            }

            // reject if any non-printable/control characters present
            if (msg.CurrentIndex < msg.MessageData.Length)
            {
                string rem = msg.MessageData.Substring(msg.CurrentIndex).TrimEnd('\r', '\n');
                Log.Logger.MinorDebug($"GC AcceptMessage: remainder='{rem}'");
                foreach (char c in rem)
                {
                    if (c < 0x20 || c > 0x7E)
                    {
                        Log.Logger.MinorDebug($"GC AcceptMessage: non-printable char detected (0x{((int)c):X2})");
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
                Log.Logger.MinorDebug($"GC AcceptMessage: exception during parse: {ex.Message}");
                // Treat parse exceptions as verification failure
                XMLParseResult = ErrorCodes.ER_01_VERIFICATION_FAILURE;
                return;
            }
            // If parser returned an empty string, treat as no-error ("00").
            XMLParseResult = string.IsNullOrEmpty(ret) ? ErrorCodes.ER_00_NO_ERROR : ret;
            Log.Logger.MinorDebug($"GC AcceptMessage: parse result={XMLParseResult}");
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();
            // If parsing or validation set a non-zero result, propagate it
            if (XMLParseResult != ErrorCodes.ER_00_NO_ERROR)
            {
                mr.AddElement(XMLParseResult);
                return mr;
            }
            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            return mr;
        }
    }
}
