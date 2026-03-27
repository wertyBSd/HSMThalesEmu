using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("SA", "SB", "", "Translates a PIN from RSA to 3DES encryption")]
    public class RSAEncryptTo3DES_SA : AHostCommand
    {
        public RSAEncryptTo3DES_SA()
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
                string srcFormat = kvp.ItemOptional("Source PIN Block Format") ?? kvp.ItemOptional("Source PIN Block Format Code") ?? "01";
                string dstFormat = kvp.ItemOptional("Destination PIN Block Format") ?? kvp.ItemOptional("Target PIN Block Format") ?? "01";
                string account = kvp.ItemOptional("Account Number") ?? string.Empty;

                string rsaHex = kvp.Item("RSA Encrypted PIN Block");
                string privateKeyFlag = kvp.ItemOptional("Private Key Flag") ?? "00";

                string clearBlockHex = rsaHex;

                // If a real RSA private key is provided via env var, attempt decryption (PKCS#1 v1.5)
                if (privateKeyFlag != "00")
                {
                    string pem = Environment.GetEnvironmentVariable("THALES_RSA_PRIVATE_KEY_PEM");
                    if (!string.IsNullOrEmpty(pem))
                    {
                        try
                        {
                            byte[] cipher = new byte[rsaHex.Length / 2];
                            Utility.HexStringToByteArray(rsaHex, cipher);
                            using (var rsa = System.Security.Cryptography.RSA.Create())
                            {
                                // Load PEM private key if possible
                                var pkcs8 = pem.Trim();
                                // .NET 5+ supports ImportFromPem
                                rsa.ImportFromPem(pkcs8.ToCharArray());
                                byte[] decrypted = rsa.Decrypt(cipher, System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);
                                string decHex = BitConverter.ToString(decrypted).Replace("-", "");
                                clearBlockHex = decHex;
                            }
                        }
                        catch (Exception)
                        {
                            mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                            return mr;
                        }
                    }
                }

                // Extract clear PIN from decrypted block
                var srcFmt = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat(srcFormat);
                var dstFmt = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat(dstFormat);

                string clearPIN = ThalesCore.PIN.PINBlockFormat.ToPIN(clearBlockHex, account, srcFmt);

                if ((clearPIN.Length < 4) || (clearPIN.Length > 12))
                {
                    mr.AddElement(ErrorCodes.ER_24_PIN_IS_FEWER_THAN_4_OR_MORE_THAN_12_DIGITS_LONG);
                    return mr;
                }

                // Create destination PIN block
                string dstBlock = ThalesCore.PIN.PINBlockFormat.ToPINBlock(clearPIN, account, dstFmt);

                // Resolve PIN Session Key (scheme + key or just key)
                string sessionKeyScheme = kvp.ItemOptional("PIN Session Key Scheme");
                string sessionKeyHex = kvp.ItemOptional("PIN Session Key") ?? string.Empty;
                string fullSessionKey = sessionKeyHex;
                if (!string.IsNullOrEmpty(sessionKeyScheme)) fullSessionKey = sessionKeyScheme + sessionKeyHex;

                if (string.IsNullOrEmpty(fullSessionKey))
                {
                    mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA); // no session key provided
                    return mr;
                }

                string cryptDst = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(fullSessionKey), dstBlock);

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
