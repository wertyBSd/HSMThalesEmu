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
    [ThalesCommandCode("MQ", "MR", "", "Generates a MAC for a large message.")]
    public class GenerateMACForLargeMessage_MQ : AHostCommand
    {
        public GenerateMACForLargeMessage_MQ()
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

            if (!kvp.ContainsKey("Message Block Number") || !kvp.ContainsKey("ZAK") || !kvp.ContainsKey("Message Length") || !kvp.ContainsKey("Message Block"))
            {
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }

            try
            {
                string blockStr = kvp.Item("Message Block Number");
                string zak = kvp.Item("ZAK");
                string iv = kvp.ItemOptional("IV");
                string msgLenHex = kvp.Item("Message Length");
                string messageHex = kvp.Item("Message Block");

                if (String.IsNullOrEmpty(iv)) iv = Constants.ZEROES;

                // Validate block string
                ISOX919Blocks block = ISOX919Blocks.OnlyBlock;
                switch (blockStr)
                {
                    case "0": block = ISOX919Blocks.OnlyBlock; break;
                    case "1": block = ISOX919Blocks.FirstBlock; break;
                    case "2": block = ISOX919Blocks.NextBlock; break;
                    case "3": block = ISOX919Blocks.FinalBlock; break;
                    default:
                        mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                        return mr;
                }

                // Message Length is hex-encoded number of bytes
                int msgLenBytes = Convert.ToInt32(msgLenHex, 16);
                int expectedHexLen = msgLenBytes * 2;

                if (String.IsNullOrEmpty(messageHex) || messageHex.Length < expectedHexLen)
                {
                    mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                    return mr;
                }

                // Trim to exact length if message is longer than declared
                if (messageHex.Length > expectedHexLen)
                    messageHex = messageHex.Substring(0, expectedHexLen);

                // Remove key-scheme prefix if present
                string keyHex = Utility.RemoveKeyType(zak);
                HexKey hk = new HexKey(keyHex);

                string mac = ISOX919MAC.MacHexData(messageHex, hk, iv, block);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(mac);
                return mr;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GenerateMACForLargeMessage_MQ Exception: " + ex.ToString());
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }
        }
    }
}
