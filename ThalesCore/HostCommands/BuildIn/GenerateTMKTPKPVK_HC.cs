using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.Cryptography;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("HC", "HD", "", "Generates a random TMK, TPK or PVK.")]
    public class GenerateTMKTPKPVK_HC : AHostCommand
    {
        public GenerateTMKTPKPVK_HC()
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

            // Key may be provided in multi-format (scheme + key) via fields 'Key Scheme' and 'Key'
            string del = kvp.ItemOptional("Delimiter");
            string keySchemeTMK = kvp.ItemOptional("Key Scheme TMK");
            string keySchemeLMK = kvp.ItemOptional("Key Scheme LMK");

            KeySchemeTable.KeyScheme ks = KeySchemeTable.KeyScheme.Unspecified; // LMK storage scheme
            KeySchemeTable.KeyScheme srcKs = KeySchemeTable.KeyScheme.Unspecified; // source key scheme

            if (del == Constants.DELIMITER_VALUE)
            {
                if (!ValidateKeySchemeCode(keySchemeLMK, ref ks, ref mr)) return mr;
                if (!ValidateKeySchemeCode(keySchemeTMK, ref srcKs, ref mr)) return mr;

                if (ks == KeySchemeTable.KeyScheme.TripleLengthKeyAnsi || ks == KeySchemeTable.KeyScheme.TripleLengthKeyVariant)
                {
                    mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME);
                    return mr;
                }
            }
            else
            {
                // default to single-length when no delimiter
                ks = KeySchemeTable.KeyScheme.SingleDESKey;
                srcKs = KeySchemeTable.KeyScheme.SingleDESKey;
            }

            try
            {
                // source key may be multi-format (scheme + key)
                string sourceKey = kvp.ItemCombination("Key Scheme", "Key").Trim();

                string clearSource = null;
                string cryptUnderSource = "";

                if (!String.IsNullOrEmpty(sourceKey))
                {
                    HexKey cryptSource = null;
                    bool parsed = false;
                    try
                    {
                        cryptSource = new HexKey(sourceKey);
                        parsed = true;
                    }
                    catch (ThalesCore.Exceptions.XInvalidKeyScheme)
                    {
                        // will try fallback below
                    }
                    catch (ThalesCore.Exceptions.XInvalidKey)
                    {
                        // will try fallback below
                    }

                    // Fallback: if the provided Key already contains a scheme prefix and the Key Scheme
                    // was also provided, ItemCombination may have duplicated the scheme char (e.g. "UU...")
                    if (!parsed && sourceKey.Length > 1 && sourceKey[0] == sourceKey[1])
                    {
                        string alt = sourceKey.Substring(1);
                        try
                        {
                            cryptSource = new HexKey(alt);
                            parsed = true;
                        }
                        catch (ThalesCore.Exceptions.XInvalidKeyScheme)
                        {
                            // fall through to error handling
                        }
                        catch (ThalesCore.Exceptions.XInvalidKey)
                        {
                            // fall through to error handling
                        }
                    }

                    if (!parsed)
                    {
                        // Determine appropriate error code based on exception type by attempting to parse an
                        // empty one-char scheme to see if it's a scheme issue; fall back to generic invalid input.
                        try
                        {
                            // try to provoke a specific exception
                            var _ = new HexKey(sourceKey);
                        }
                        catch (ThalesCore.Exceptions.XInvalidKeyScheme)
                        {
                            mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME);
                            return mr;
                        }
                        catch (ThalesCore.Exceptions.XInvalidKey)
                        {
                            mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                            return mr;
                        }

                        // if we reach here, generic unknown format
                        mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                        return mr;
                    }

                    clearSource = Utility.DecryptUnderLMK(cryptSource.ToString(), cryptSource.Scheme, LMKPairs.LMKPair.Pair04_05, "0");

                    if (!Utility.IsParityOK(clearSource, Utility.ParityCheck.OddParity))
                    {
                        mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                        return mr;
                    }
                }

                // Generate random key according to source key scheme
                string clearKey = Utility.CreateRandomKey(srcKs);
                string cryptUnderLMK = Utility.EncryptUnderLMK(clearKey, ks, LMKPairs.LMKPair.Pair06_07, "0");
                string chkVal = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(clearKey), Constants.ZEROES);

                if (!String.IsNullOrEmpty(sourceKey) && !String.IsNullOrEmpty(clearSource))
                {
                    cryptUnderSource = Utility.EncryptUnderZMK(clearSource, clearKey, srcKs);
                    Log.Logger.MinorInfo("Source (clear): " + clearSource);
                    Log.Logger.MinorInfo("Key (under source): " + cryptUnderSource);
                }

                Log.Logger.MinorInfo("Generated key (clear): " + clearKey);
                Log.Logger.MinorInfo("Generated key (LMK): " + cryptUnderLMK);
                Log.Logger.MinorInfo("Check value: " + chkVal.Substring(0, 6));

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(cryptUnderLMK);
                mr.AddElement(cryptUnderSource);
                mr.AddElement(chkVal.Substring(0, 6));

                return mr;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GenerateTMKTPKPVK_HC Exception: " + ex.ToString());
                mr.AddElement(ErrorCodes.ER_ZZ_UNKNOWN_ERROR);
                return mr;
            }
        }
    }
}
