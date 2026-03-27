using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("BU", "BV", "", "Generate a key check value.")]
    public class GenerateCheckValue_BU : AHostCommand
    {
        public GenerateCheckValue_BU()
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

            if (!kvp.ContainsKey("Key"))
            {
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }

            string key = kvp.Item("Key");
            string keyCheckType = kvp.ItemOptional("Key Check Value Type");

            try
            {
                // Parse key (handles optional scheme prefix)
                var hk = new ThalesCore.Cryptography.HexKey(key);
                string clearKey = hk.ToString();

                if (!Utility.IsParityOK(clearKey, Utility.ParityCheck.OddParity))
                {
                    mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                    return mr;
                }

                string chkVal = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(clearKey), ThalesCore.HostCommands.Constants.ZEROES);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);

                if (keyCheckType == "0")
                    mr.AddElement(chkVal);
                else
                    mr.AddElement(chkVal.Substring(0, 6));

                return mr;
            }
            catch (Exception)
            {
                mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                return mr;
            }
        }
    }
}
