using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;
using ThalesCore.Cryptography;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("FG", "FH", "", "Generates two random keys.")]
    public class GeneratePVKPair_FG : AHostCommand
    {
        public GeneratePVKPair_FG()
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

            string del = kvp.ItemOptional("Delimiter");
            string keySchemeZMK = kvp.ItemOptional("Key Scheme ZMK");
            string keySchemeLMK = kvp.ItemOptional("Key Scheme LMK");
            string keyCheck = kvp.ItemOptional("Key Check Value Type") ?? "0";

            KeySchemeTable.KeyScheme ks = KeySchemeTable.KeyScheme.Unspecified;
            KeySchemeTable.KeyScheme zmkKs = KeySchemeTable.KeyScheme.Unspecified;

            if (del == Constants.DELIMITER_VALUE)
            {
                if (!ValidateKeySchemeCode(keySchemeLMK, ref ks, ref mr)) return mr;
                if (!ValidateKeySchemeCode(keySchemeZMK, ref zmkKs, ref mr)) return mr;

                // disallow certain combinations similar to ZPK generation
                if (ks == KeySchemeTable.KeyScheme.TripleLengthKeyAnsi || ks == KeySchemeTable.KeyScheme.TripleLengthKeyVariant)
                {
                    mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME);
                    return mr;
                }
            }
            else
            {
                // defaults when delimiter not present
                ks = KeySchemeTable.KeyScheme.SingleDESKey;
                zmkKs = KeySchemeTable.KeyScheme.SingleDESKey;
                keyCheck = "0";
            }

            try
            {
                // source ZMK may be multi-format (scheme + key)
                string sourceZmk = kvp.ItemCombination("ZMK Scheme", "ZMK").Trim();

                if (string.IsNullOrEmpty(sourceZmk))
                {
                    mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                    return mr;
                }

                HexKey cryptSource = null;
                try
                {
                    cryptSource = new HexKey(sourceZmk);
                }
                catch (Exceptions.XInvalidKeyScheme)
                {
                    mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME);
                    return mr;
                }
                catch (Exceptions.XInvalidKey)
                {
                    mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                    return mr;
                }

                string clearSource = Utility.DecryptUnderLMK(cryptSource.ToString(), cryptSource.Scheme, LMKPairs.LMKPair.Pair04_05, "0");

                if (!Utility.IsParityOK(clearSource, Utility.ParityCheck.OddParity))
                {
                    mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                    return mr;
                }

                // Validate compatibility of ZMK and LMK schemes
                if (ks == KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi || ks == KeySchemeTable.KeyScheme.DoubleLengthKeyVariant)
                {
                    if (zmkKs == KeySchemeTable.KeyScheme.SingleDESKey || zmkKs == KeySchemeTable.KeyScheme.TripleLengthKeyAnsi || zmkKs == KeySchemeTable.KeyScheme.TripleLengthKeyVariant)
                    {
                        mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                        return mr;
                    }
                }
                else
                {
                    if (zmkKs != KeySchemeTable.KeyScheme.SingleDESKey)
                    {
                        mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                        return mr;
                    }
                }

                // Generate two random keys under ZMK key scheme
                string clearKey1 = Utility.CreateRandomKey(zmkKs);
                string clearKey2 = Utility.CreateRandomKey(zmkKs);

                // Encrypt under ZMK and LMK
                string cryptKey1ZMK = Utility.EncryptUnderZMK(clearSource, clearKey1, zmkKs);
                string cryptKey2ZMK = Utility.EncryptUnderZMK(clearSource, clearKey2, zmkKs);

                // Encrypt under LMK (store scheme = ks) using LMK pair 06_07 as per ZPK conventions
                string cryptKey1LMK = Utility.EncryptUnderLMK(clearKey1, ks, LMKPairs.LMKPair.Pair06_07, "0");
                string cryptKey2LMK = Utility.EncryptUnderLMK(clearKey2, ks, LMKPairs.LMKPair.Pair06_07, "0");

                string chk1 = TripleDES.TripleDESEncrypt(new HexKey(clearKey1), Constants.ZEROES);
                string chk2 = TripleDES.TripleDESEncrypt(new HexKey(clearKey2), Constants.ZEROES);

                Log.Logger.MinorInfo("ZMK (clear): " + clearSource);
                Log.Logger.MinorInfo("PVK1 (clear): " + clearKey1);
                Log.Logger.MinorInfo("PVK2 (clear): " + clearKey2);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);

                // Return ZMK-encrypted keys first
                mr.AddElement(cryptKey1ZMK);
                mr.AddElement(cryptKey2ZMK);

                // Then LMK-encrypted keys
                mr.AddElement(cryptKey1LMK);
                mr.AddElement(cryptKey2LMK);

                // Then check values according to type
                if (keyCheck == "0")
                {
                    mr.AddElement(chk1);
                    mr.AddElement(chk2);
                }
                else
                {
                    mr.AddElement(chk1.Substring(0, 6));
                    mr.AddElement(chk2.Substring(0, 6));
                }

                return mr;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GeneratePVKPair_FG Exception: " + ex.ToString());
                mr.AddElement(ErrorCodes.ER_ZZ_UNKNOWN_ERROR);
                return mr;
            }
        }
    }
}
