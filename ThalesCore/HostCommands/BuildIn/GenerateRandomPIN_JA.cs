using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using System.Security.Cryptography;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("JA", "JB", "", "Generates a random PIN of 4 to 12 digits.")]
    public class GenerateRandomPIN_JA : AHostCommand
    {
        public GenerateRandomPIN_JA()
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

            // retrieve fields
            string account = kvp.ItemOptional("Account Number");
            string pinLenStr = kvp.ItemOptional("PIN Length");

            // basic validation: Account Number must be 12 numeric digits
            if (string.IsNullOrEmpty(account) || !System.Text.RegularExpressions.Regex.IsMatch(account, "^[0-9]{12}$"))
            {
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }

            if (string.IsNullOrEmpty(pinLenStr) || pinLenStr.Length != 2 || !int.TryParse(pinLenStr, out int pinLen))
            {
                mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                return mr;
            }

            if (pinLen < 4 || pinLen > 12)
            {
                mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                return mr;
            }

            // generate random numeric PIN of requested length using a cryptographically secure RNG
            var sb = new System.Text.StringBuilder();
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] b = new byte[1];
                for (int i = 0; i < pinLen; i++)
                {
                    rng.GetBytes(b);
                    sb.Append((b[0] % 10).ToString());
                }
            }

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(sb.ToString());
            return mr;
        }
    }
}
