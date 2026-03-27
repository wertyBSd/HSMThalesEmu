using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("BA", "BB", "", "Encrypts a clear PIN.")]
    public class EncryptClearPIN_BA : AHostCommand
    {
        public EncryptClearPIN_BA()
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
                string pinField = kvp.ItemOptional("PIN") ?? string.Empty;
                if (string.IsNullOrEmpty(pinField))
                {
                    mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                    return mr;
                }

                // Normalize: if a leading length nibble was included, strip it to get full block
                string clearBlockHex = pinField;
                if ((clearBlockHex.Length % 2) == 1 && clearBlockHex.Length > 0 && Char.IsDigit(clearBlockHex[0]))
                    clearBlockHex = clearBlockHex.Substring(1);

                // Ensure we have an even-length hex string
                if (!ThalesCore.Utility.IsHexString(clearBlockHex) || (clearBlockHex.Length % 2) != 0)
                {
                    mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                    return mr;
                }

                // Encrypt under LMK pair 02-03
                string lmkKey = ThalesCore.Cryptography.LMK.LMKStorage.LMKVariant(ThalesCore.LMKPairs.LMKPair.Pair02_03, 0);
                string crypt = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(lmkKey), clearBlockHex);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(crypt);
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
