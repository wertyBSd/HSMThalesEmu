using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("CC", "CD", "", "Translates a PIN block from ZPK to ZPK encryption.")]
    public class TranslatePINFromZPKToZPK_CC : AHostCommand
    {
        public TranslatePINFromZPKToZPK_CC()
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
                string sourceZpk = kvp.Item("Source ZPK");
                string destZpk = kvp.Item("Destination ZPK");
                string maxPinLenStr = kvp.Item("Max PIN Length");
                string srcPinBlock = kvp.Item("Source PIN Block");
                string srcFormat = kvp.Item("Source PIN Block Format Code");
                string dstFormat = kvp.Item("Destination PIN Block Format Code");
                string account = kvp.Item("Account Number");

                var srcFmt = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat(srcFormat);
                var dstFmt = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat(dstFormat);

                // Decrypt source PIN block under source ZPK
                string decBlock = ThalesCore.Cryptography.TripleDES.TripleDESDecrypt(new ThalesCore.Cryptography.HexKey(sourceZpk), srcPinBlock);

                // Extract clear PIN
                string clearPIN = ThalesCore.PIN.PINBlockFormat.ToPIN(decBlock, account, srcFmt);

                int maxPinLen = Convert.ToInt32(maxPinLenStr);
                if ((clearPIN.Length < 4) || (clearPIN.Length > 12) || (clearPIN.Length > maxPinLen))
                {
                    mr.AddElement(ErrorCodes.ER_24_PIN_IS_FEWER_THAN_4_OR_MORE_THAN_12_DIGITS_LONG);
                    return mr;
                }

                // Create destination PIN block
                string dstBlock = ThalesCore.PIN.PINBlockFormat.ToPINBlock(clearPIN, account, dstFmt);

                // Encrypt destination block under destination ZPK
                string cryptDst = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(destZpk), dstBlock);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(cryptDst);
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
