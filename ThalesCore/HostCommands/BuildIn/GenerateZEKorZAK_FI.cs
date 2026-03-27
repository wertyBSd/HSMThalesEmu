using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.Cryptography;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("FI", "FJ", "", "Generates a ZEK or ZAK.")]
    public class GenerateZEKorZAK_FI : AHostCommand
    {
        private string _flag;
        private string _sourceZmk;
        private string _del;
        private string _keySchemeZMK;
        private string _keySchemeLMK;
        private string _keyCheckValue;

        public GenerateZEKorZAK_FI()
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
            KeySchemeTable.KeyScheme ks = new KeySchemeTable.KeyScheme();
            KeySchemeTable.KeyScheme zmkKs = new KeySchemeTable.KeyScheme();

            _flag = kvp.ItemOptional("Flag");
            _sourceZmk = kvp.ItemCombination("ZMK Scheme", "ZMK");
            _del = kvp.ItemOptional("Delimiter");
            _keySchemeZMK = kvp.ItemOptional("Key Scheme ZMK");
            _keySchemeLMK = kvp.ItemOptional("Key Scheme LMK");
            _keyCheckValue = kvp.ItemOptional("Key Check Value Type");

            if (_del == Constants.DELIMITER_VALUE)
            {
                if (!ValidateKeySchemeCode(_keySchemeLMK, ref ks, ref mr))
                {
                    return mr;
                }
                if (!ValidateKeySchemeCode(_keySchemeZMK, ref zmkKs, ref mr))
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
                ks = KeySchemeTable.KeyScheme.SingleDESKey;
                zmkKs = KeySchemeTable.KeyScheme.SingleDESKey;
                _keyCheckValue = "0";
            }

            if (string.IsNullOrEmpty(_sourceZmk))
            {
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }

            HexKey cryptSource = null;
            try
            {
                cryptSource = new HexKey(_sourceZmk.Trim());
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

            string clearSource = Utility.DecryptUnderLMK(cryptSource.ToString(), cryptSource.Scheme, LMKPairs.LMKPair.Pair04_05, "0");

            if (!Utility.IsParityOK(clearSource, Utility.ParityCheck.OddParity))
            {
                mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                return mr;
            }
            else if (ks == KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi || ks == KeySchemeTable.KeyScheme.DoubleLengthKeyVariant)
            {
                if (zmkKs == KeySchemeTable.KeyScheme.SingleDESKey || zmkKs == KeySchemeTable.KeyScheme.TripleLengthKeyAnsi
                    || zmkKs == KeySchemeTable.KeyScheme.TripleLengthKeyVariant)
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

            string clearKey = Utility.CreateRandomKey(zmkKs);

            string cryptKeyZMK = Utility.EncryptUnderZMK(clearSource, clearKey, zmkKs);
            string cryptKeyLMK = Utility.EncryptUnderLMK(clearKey, ks, LMKPairs.LMKPair.Pair06_07, "0");
            string checkValue = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(clearKey), Constants.ZEROES);

            Log.Logger.MinorInfo("ZMK (clear): " + clearSource);
            Log.Logger.MinorInfo("ZEK/ZAK (clear): " + clearKey);
            Log.Logger.MinorInfo("ZEK/ZAK (ZMK): " + cryptKeyZMK);
            Log.Logger.MinorInfo("ZEK/ZAK (LMK): " + cryptKeyLMK);

            if (_keyCheckValue == "0")
            {
                Log.Logger.MinorInfo("Check value: " + checkValue);
            }
            else
            {
                Log.Logger.MinorInfo("Check value: " + checkValue.Substring(0, 6));
            }

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(cryptKeyZMK);
            mr.AddElement(cryptKeyLMK);

            if (_keyCheckValue == "0")
            {
                mr.AddElement(checkValue);
            }
            else
            {
                mr.AddElement(checkValue.Substring(0, 6));
            }

            return mr;
        }
    }
}
