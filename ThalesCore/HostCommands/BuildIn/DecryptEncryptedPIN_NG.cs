using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("NG", "NH", "", "Decrypt an encrypted PIN.")]
    public class DecryptEncryptedPIN_NG : AHostCommand
    {
        private string _rawMessage = string.Empty;
        public DecryptEncryptedPIN_NG()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            string ret = string.Empty;
            ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            _rawMessage = msg?.MessageData ?? string.Empty;
            XMLParseResult = ret;
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();
            try
            {
                string pinBlockLMK = kvp.ItemOptional("PIN") ?? string.Empty;
                string account = kvp.ItemOptional("Account Number") ?? string.Empty;
                var raw = _rawMessage ?? string.Empty;
                // If parser returned a non-empty PIN but it contains non-numeric characters
                // it's likely the XML parser sliced into the LMK-encrypted block. Prefer
                // extracting the full block from the raw message (account+block).
                if (!string.IsNullOrEmpty(pinBlockLMK) && !System.Text.RegularExpressions.Regex.IsMatch(pinBlockLMK, "^\\d+$"))
                {
                    if (raw.Length > 12)
                    {
                        pinBlockLMK = raw.Substring(12);
                        if (string.IsNullOrEmpty(account)) account = raw.Substring(0, 12);
                    }
                }
                // If parser didn't populate PIN at all, also try raw extraction
                if (string.IsNullOrEmpty(pinBlockLMK) && raw.Length > 12)
                {
                    pinBlockLMK = raw.Substring(12);
                    if (string.IsNullOrEmpty(account)) account = raw.Substring(0, 12);
                }

                // Decrypt PIN block under LMK pair 02-03 (internal storage variant)
                string lmkKey = ThalesCore.Cryptography.LMK.LMKStorage.LMKVariant(ThalesCore.LMKPairs.LMKPair.Pair02_03, 0);
                // Attempt decryption and PIN extraction. Try both possible raw layouts if needed.
                string clearPIN = string.Empty;
                try
                {
                    string tryPinBlock = pinBlockLMK;
                    string tryAccount = account;

                    if (!string.IsNullOrEmpty(tryPinBlock))
                    {
                        
                        // If pin block is length-prefixed (leading length digit), strip it for decryption.
                        // Only strip when the overall hex string length is odd (indicating a leading length nibble).
                        if ((tryPinBlock.Length % 2) == 1 && tryPinBlock.Length > 0 && Char.IsDigit(tryPinBlock[0]))
                            tryPinBlock = tryPinBlock.Substring(1);

                        string clearBlock = ThalesCore.Cryptography.TripleDES.TripleDESDecrypt(new ThalesCore.Cryptography.HexKey(lmkKey), tryPinBlock);
                        
                        var fmt = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat("01");
                        clearPIN = ThalesCore.PIN.PINBlockFormat.ToPIN(clearBlock, tryAccount, fmt);
                    }

                    // If extraction failed or returned empty PIN, try the alternate raw layout: PIN+Account
                    if (string.IsNullOrEmpty(clearPIN) && !string.IsNullOrEmpty(_rawMessage) && _rawMessage.Length > 12)
                    {
                        string altAccount = raw.Substring(raw.Length - 12);
                        string altPinBlock = raw.Substring(0, raw.Length - 12);
                        if ((altPinBlock.Length % 2) == 1 && altPinBlock.Length > 0 && Char.IsDigit(altPinBlock[0]))
                            altPinBlock = altPinBlock.Substring(1);

                        string clearBlock2 = ThalesCore.Cryptography.TripleDES.TripleDESDecrypt(new ThalesCore.Cryptography.HexKey(lmkKey), altPinBlock);
                        var fmt2 = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat("01");
                        clearPIN = ThalesCore.PIN.PINBlockFormat.ToPIN(clearBlock2, altAccount, fmt2);
                        account = altAccount;
                    }

                    if (string.IsNullOrEmpty(clearPIN))
                    {
                        mr.AddElement(ErrorCodes.ER_20_PIN_BLOCK_DOES_NOT_CONTAIN_VALID_VALUES);
                        return mr;
                    }

                    if ((clearPIN.Length < 4) || (clearPIN.Length > 12))
                    {
                        mr.AddElement(ErrorCodes.ER_24_PIN_IS_FEWER_THAN_4_OR_MORE_THAN_12_DIGITS_LONG);
                        return mr;
                    }
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

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(clearPIN);
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
