using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;
using ThalesCore.Cryptography;
using ThalesCore.Cryptography.MAC;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("MA", "MB", "", "Generates a MAC.")]
    public class GenerateMAC_MA : AHostCommand
    {
        public GenerateMAC_MA()
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

            if (!kvp.ContainsKey("TAC") || !kvp.ContainsKey("Data"))
            {
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }

            try
            {
                string tac = kvp.Item("TAC");
                string data = kvp.Item("Data");

                // TAC may include key-scheme prefix
                string keyHex = Utility.RemoveKeyType(tac);
                HexKey hk = new HexKey(keyHex);

                // Data field is binary (hex string). Compute ISO X9.19 MAC over hex data.
                string iv = Constants.ZEROES;
                string mac = ISOX919MAC.MacHexData(data, hk, iv, ISOX919Blocks.OnlyBlock);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(mac);
                return mr;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GenerateMAC_MA Exception: " + ex.ToString());
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }
        }
    }
}
