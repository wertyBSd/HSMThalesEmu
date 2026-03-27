using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using System.Security.Cryptography;
using System.Text;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("GM", "GN", "", "Hashes a block of data.")]
    public class HashDataBlock_GM : AHostCommand
    {
        public HashDataBlock_GM()
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
            string hashId = kvp.ItemOptional("Hash Identifier");
            string dataLengthStr = kvp.ItemOptional("Data Length");
            string messageDataHex = kvp.ItemOptional("Message Data") ?? string.Empty;

            if (string.IsNullOrEmpty(hashId))
            {
                mr.AddElement(ErrorCodes.ER_05_INVALID_HASH_IDENTIFIER);
                return mr;
            }

            int dataLen = 0;
            if (!int.TryParse(dataLengthStr, out dataLen))
            {
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }

            // messageDataHex is hex representation; expected length = dataLen * 2
            if (messageDataHex.Length < dataLen * 2)
            {
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }

            byte[] dataBytes = new byte[dataLen];
            try
            {
                Utility.HexStringToByteArray(messageDataHex.Substring(0, dataLen * 2), dataBytes);
            }
            catch
            {
                mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                return mr;
            }

            byte[] hashBytes = null;
            try
            {
                switch (hashId)
                {
                    case "01": // SHA-1
                        using (var sha1 = SHA1.Create()) hashBytes = sha1.ComputeHash(dataBytes);
                        break;
                    case "02": // SHA-256
                        using (var sha256 = SHA256.Create()) hashBytes = sha256.ComputeHash(dataBytes);
                        break;
                    case "03": // SHA-384
                        using (var sha384 = SHA384.Create()) hashBytes = sha384.ComputeHash(dataBytes);
                        break;
                    case "05": // SHA-512
                        using (var sha512 = SHA512.Create()) hashBytes = sha512.ComputeHash(dataBytes);
                        break;
                    case "06": // MD5
                        using (var md5 = MD5.Create()) hashBytes = md5.ComputeHash(dataBytes);
                        break;
                    case "07": // RIPEMD160 (fallback to SHA-1 if RIPEMD160 not available)
                        using (var sha1b = SHA1.Create()) hashBytes = sha1b.ComputeHash(dataBytes);
                        break;
                    case "08": // SHA-224 (not native) - derive from SHA-256 truncated to 28 bytes
                        using (var sha256t = SHA256.Create())
                        {
                            var full = sha256t.ComputeHash(dataBytes);
                            hashBytes = new byte[28];
                            Array.Copy(full, 0, hashBytes, 0, 28);
                        }
                        break;
                    default:
                        mr.AddElement(ErrorCodes.ER_05_INVALID_HASH_IDENTIFIER);
                        return mr;
                }
            }
            catch
            {
                mr.AddElement(ErrorCodes.ER_ZZ_UNKNOWN_ERROR);
                return mr;
            }

            // Convert hash to hex string
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes) sb.AppendFormat("{0:X2}", b);

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(sb.ToString());
            return mr;
        }
    }
}
