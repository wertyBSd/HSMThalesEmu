using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("M6", "M7", "", "Generate a MAC on a message using a TAK or ZAK.")]
    public class GenerateMAC_M6 : AHostCommand
    {
        public GenerateMAC_M6()
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

            if (!kvp.ContainsKey("Key") || !kvp.ContainsKey("Message"))
            {
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }

            try
            {
                string modeFlag = kvp.ItemOptional("Mode Flag") ?? "0";
                string inputFormat = kvp.ItemOptional("Input Format Flag") ?? "0";
                string macAlg = kvp.ItemOptional("MAC Algorithm") ?? "01";
                string padding = kvp.ItemOptional("Padding Method") ?? "0";
                string key = kvp.Item("Key");
                string iv = kvp.ItemOptional("IV");
                if (String.IsNullOrEmpty(iv)) iv = "0000000000000000";
                string message = kvp.Item("Message");

                // Normalize key (HexKey accepts optional scheme prefix)
                var hk = new ThalesCore.Cryptography.HexKey(key);
                string clearKey = hk.ToString();

                if (!Utility.IsParityOK(clearKey, Utility.ParityCheck.OddParity))
                {
                    mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                    return mr;
                }

                // Build message bytes according to Input Format Flag
                byte[] msgBytes;
                if (inputFormat == "1")
                {
                    // hex
                    int byteLen = message.Length / 2;
                    msgBytes = new byte[byteLen];
                    Utility.HexStringToByteArray(message, msgBytes);
                }
                else
                {
                    // treat as ASCII/character
                    msgBytes = System.Text.Encoding.ASCII.GetBytes(message);
                }

                // Apply padding to 8-byte blocks
                int blockSize = 8;
                int remainder = msgBytes.Length % blockSize;
                System.Collections.Generic.List<byte> mb = new System.Collections.Generic.List<byte>(msgBytes);
                if (remainder != 0)
                {
                    if (padding == "1")
                    {
                        // 0x80 then zeros
                        mb.Add(0x80);
                        while (mb.Count % blockSize != 0) mb.Add(0x00);
                    }
                    else
                    {
                        // padding 0 (zeros)
                        while (mb.Count % blockSize != 0) mb.Add(0x00);
                    }
                }

                // Compute MAC: CBC encrypt with TripleDES (block chaining), return leftmost 8 hex chars
                string currentIV = iv;
                string finalEnc = "";
                for (int i = 0; i < mb.Count; i += blockSize)
                {
                    byte[] blk = mb.GetRange(i, blockSize).ToArray();
                    // convert block to hex
                    string blkHex = BitConverter.ToString(blk).Replace("-", "");
                    // xor with IV
                    string xored = Utility.XORHexStringsFull(blkHex, currentIV);
                    // encrypt under key
                    finalEnc = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(clearKey), xored);
                    currentIV = finalEnc;
                }

                string mac = (finalEnc.Length >= 8) ? finalEnc.Substring(0, 8) : finalEnc;

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(mac);
                return mr;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GenerateMAC_M6 Exception: " + ex.ToString());
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }
        }
    }
}
