using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("DE", "DF", "", "Generates an IBM PIN Offset.")]
    public class GenerateIBMOffset_DE : AHostCommand
    {
        public GenerateIBMOffset_DE()
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
                // Extract fields
                string pvk = kvp.ItemOptional("PVK") ?? string.Empty;
                string pin = kvp.ItemOptional("PIN") ?? string.Empty;
                string checkLenStr = kvp.ItemOptional("Check Length") ?? "4";
                string account = kvp.ItemOptional("Account Number") ?? string.Empty;
                string decTable = kvp.ItemOptional("Decimalisation Table") ?? string.Empty;
                string pvd = kvp.ItemOptional("PIN Validation Data") ?? string.Empty;

                if (string.IsNullOrEmpty(pvk) || string.IsNullOrEmpty(pvd) || string.IsNullOrEmpty(pin))
                {
                    mr.AddElement(ErrorCodes.ER_20_PIN_BLOCK_DOES_NOT_CONTAIN_VALID_VALUES);
                    return mr;
                }

                int checkLen = 4;
                if (!int.TryParse(checkLenStr, out checkLen) || checkLen < 1 || checkLen > 12) checkLen = 4;

                // Build 16-digit numeric input from PIN Validation Data (pad with zeros)
                string inputDigits = pvd.PadRight(12, '0') + "0000"; // ensure 16 digits

                // Convert to BCD hex string (each pair of digits -> one byte)
                string bcdHex = "";
                for (int i = 0; i < 16; i += 2)
                {
                    int hi = inputDigits[i] - '0';
                    int lo = inputDigits[i + 1] - '0';
                    int val = (hi << 4) | lo;
                    bcdHex += val.ToString("X2");
                }

                // Remove possible key-type prefix and use provided PVK as hex
                string keyHex = ThalesCore.Utility.RemoveKeyType(pvk);

                // Encrypt the BCD block under PVK
                string encHex = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(keyHex), bcdHex);

                // Decimalise the encrypted hex using provided decimalisation table
                string decimalised = ThalesCore.Utility.Decimalise(encHex, decTable);

                // Natural value is first checkLen digits
                if (decimalised.Length < checkLen)
                {
                    mr.AddElement(ErrorCodes.ER_20_PIN_BLOCK_DOES_NOT_CONTAIN_VALID_VALUES);
                    return mr;
                }

                string natural = decimalised.Substring(0, checkLen);

                if (pin.Length < checkLen)
                {
                    mr.AddElement(ErrorCodes.ER_20_PIN_BLOCK_DOES_NOT_CONTAIN_VALID_VALUES);
                    return mr;
                }

                // Compute offset = (PIN - natural) mod 10 per digit
                char[] offChars = new char[checkLen];
                for (int i = 0; i < checkLen; i++)
                {
                    int pd = pin[i] - '0';
                    int nd = natural[i] - '0';
                    int offset = (pd - nd + 10) % 10;
                    offChars[i] = (char)('0' + offset);
                }

                string offsetStr = new string(offChars);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(offsetStr);
                return mr;
            }
            catch (Exception)
            {
                mr.AddElement(ErrorCodes.ER_20_PIN_BLOCK_DOES_NOT_CONTAIN_VALID_VALUES);
                return mr;
            }
        }
    }
}
