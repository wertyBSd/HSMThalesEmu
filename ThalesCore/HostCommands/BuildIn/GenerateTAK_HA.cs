using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Cryptography;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("HA", "HB", "", "Generates a TAK.")]
    public class GenerateTAK_HA : AHostCommand
    {
        private string _tmk;
        private string _del;
        private string _keySchemeTMK;
        private string _keySchemeLMK;

        public GenerateTAK_HA()
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
            KeySchemeTable.KeyScheme tmkKs = new KeySchemeTable.KeyScheme();
            KeySchemeTable.KeyScheme ks = new KeySchemeTable.KeyScheme();

            // read parsed fields from kvp (XML parser already populated kvp in AcceptMessage)
            _tmk = kvp.ItemOptional("TMK");
            _del = kvp.ItemOptional("Delimiter");
            _keySchemeTMK = kvp.ItemOptional("Key Scheme TMK");
            _keySchemeLMK = kvp.ItemOptional("Key Scheme LMK");

            if (_del == Constants.DELIMITER_VALUE)
            {
                if (!ValidateKeySchemeCode(_keySchemeLMK, ref ks, ref mr))
                {
                    return mr;
                }
                if (!ValidateKeySchemeCode(_keySchemeTMK, ref tmkKs, ref mr))
                {
                    return mr;
                }

                if (ks == KeySchemeTable.KeyScheme.TripleLengthKeyAnsi || ks == KeySchemeTable.KeyScheme.TripleLengthKeyVariant)
                {
                    mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME);
                    return mr;
                }
            }
            else
            {
                // defaults when no delimiter: single-length
                ks = KeySchemeTable.KeyScheme.SingleDESKey;
                tmkKs = KeySchemeTable.KeyScheme.SingleDESKey;
            }

            string clearSource = null;
            string cryptUnderTMK = "";

            if (!String.IsNullOrEmpty(_tmk))
            {
                string sourceTmk = _tmk.Trim();
                if (string.IsNullOrEmpty(sourceTmk))
                {
                    mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                    return mr;
                }

                HexKey cryptTMK = null;
                try
                {
                    cryptTMK = new HexKey(sourceTmk);
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

                clearSource = Utility.DecryptUnderLMK(cryptTMK.ToString(), cryptTMK.Scheme, LMKPairs.LMKPair.Pair04_05, "0");

                if (!Utility.IsParityOK(clearSource, Utility.ParityCheck.OddParity))
                {
                    mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                    return mr;
                }
            }

            string clearKey = Utility.CreateRandomKey(tmkKs);

            if (!String.IsNullOrEmpty(_tmk) && !String.IsNullOrEmpty(clearSource))
            {
                cryptUnderTMK = Utility.EncryptUnderZMK(clearSource, clearKey, tmkKs);
                Log.Logger.MinorInfo("TMK (clear): " + clearSource);
                Log.Logger.MinorInfo("TAK (TMK): " + cryptUnderTMK);
            }

            string cryptUnderLMK = Utility.EncryptUnderLMK(clearKey, ks, LMKPairs.LMKPair.Pair06_07, "0");
            string checkValue = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(clearKey), Constants.ZEROES);

            Log.Logger.MinorInfo("TAK (clear): " + clearKey);
            Log.Logger.MinorInfo("TAK (LMK): " + cryptUnderLMK);
            Log.Logger.MinorInfo("Check value: " + checkValue.Substring(0, 6));

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(cryptUnderTMK);
            mr.AddElement(cryptUnderLMK);
            mr.AddElement(checkValue.Substring(0, 6));

            return mr;
        }
    }
}
