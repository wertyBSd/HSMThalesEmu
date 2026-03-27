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
    [ThalesCommandCode("MS", "MT", "", "Generate MAC (MAB) using ANSI X9.19 for large message.")]
    public class GeneraceMACMABUsingAnsiX919ForLargeMessage_MS : AHostCommand
    {
        public GeneraceMACMABUsingAnsiX919ForLargeMessage_MS()
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

            try
            {
                string blockStr = kvp.Item("Message Block");
                string keyType = kvp.Item("Key Type");
                string keyLenFlag = kvp.Item("Key Length");
                string msgType = kvp.Item("Message Type");
                string key = kvp.Item("Key");
                string iv = kvp.ItemOptional("IV");
                string msgLenHex = kvp.Item("Message Length");
                string message = kvp.Item("Message");

                // Validate single-character flags per XML spec (allowed values are '0' or '1')
                if (!(keyType == "0" || keyType == "1"))
                {
                    mr.AddElement(ErrorCodes.ER_04_INVALID_KEY_TYPE_CODE);
                    return mr;
                }

                if (!(keyLenFlag == "0" || keyLenFlag == "1"))
                {
                    mr.AddElement(ErrorCodes.ER_05_INVALID_KEY_LENGTH_FLAG);
                    return mr;
                }

                if (!(msgType == "0" || msgType == "1"))
                {
                    mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                    return mr;
                }

                if (String.IsNullOrEmpty(iv)) iv = Constants.ZEROES;

                // Convert message (character data) to hex string
                string hexMessage = String.Empty;
                Utility.ByteArrayToHexString(Utility.GetBytesFromString(message), out hexMessage);

                ISOX919Blocks block = ISOX919Blocks.OnlyBlock;
                switch (blockStr)
                {
                    case "0": block = ISOX919Blocks.OnlyBlock; break;
                    case "1": block = ISOX919Blocks.FirstBlock; break;
                    case "2": block = ISOX919Blocks.NextBlock; break;
                    case "3": block = ISOX919Blocks.FinalBlock; break;
                    default: block = ISOX919Blocks.OnlyBlock; break;
                }

                // Remove key-scheme prefix if present
                string keyHex = Utility.RemoveKeyType(key);

                HexKey hk = new HexKey(keyHex);

                string mac = ISOX919MAC.MacHexData(hexMessage, hk, iv, block);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(mac);
                return mr;
            }
            catch (Exception)
            {
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }
        }
    }
}
