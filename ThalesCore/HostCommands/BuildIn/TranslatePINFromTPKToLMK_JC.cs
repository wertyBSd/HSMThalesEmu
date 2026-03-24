using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("JC", "JD", "", "Translates a PIN from TPK to LMK encryption.")]
    public class TranslatePINFromTPKToLMK_JC : AHostCommand
    {
        public TranslatePINFromTPKToLMK_JC()
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
                string tpk = kvp.Item("TPK");
                string pinBlock = kvp.Item("PIN Block");
                string format = kvp.Item("PIN Block Format Code");
                string account = kvp.Item("Account Number");

                var fmt = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat(format);

                // Decrypt PIN block under TPK
                string decBlock = ThalesCore.Cryptography.TripleDES.TripleDESDecrypt(new ThalesCore.Cryptography.HexKey(tpk), pinBlock);
                string clearPIN = ThalesCore.PIN.PINBlockFormat.ToPIN(decBlock, account, fmt);

                if ((clearPIN.Length < 4) || (clearPIN.Length > 12))
                {
                    mr.AddElement(ErrorCodes.ER_24_PIN_IS_FEWER_THAN_4_OR_MORE_THAN_12_DIGITS_LONG);
                    return mr;
                }

                // Build LMK-encrypted PIN block (encrypt clear block under LMK pair 02-03)
                string clearBlock = ThalesCore.PIN.PINBlockFormat.ToPINBlock(clearPIN, account, fmt);
                string lmkKey = ThalesCore.Cryptography.LMK.LMKStorage.LMKVariant(ThalesCore.LMKPairs.LMKPair.Pair02_03, 0);
                string cryptUnderLMK = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(lmkKey), clearBlock);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(cryptUnderLMK);
                return mr;
            }
            catch (ThalesCore.Exceptions.XUnsupportedPINBlockFormat)
            {
                mr.AddElement(ErrorCodes.ER_23_INVALID_PIN_BLOCK_FORMAT_CODE);
                return mr;
            }
            catch (ThalesCore.Exceptions.XInvalidAccount)
            {
                mr.AddElement(ErrorCodes.ER_20_PIN_BLOCK_DOES_NOT_CONTAIN_VALID_VALUES);
                return mr;
            }
            catch (Exception)
            {
                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                return mr;
            }
        }
    }
}
