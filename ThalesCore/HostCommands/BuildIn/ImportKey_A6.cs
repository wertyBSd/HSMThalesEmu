using HostCommands;
using System;
using ThalesCore.Cryptography;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("A6", "A7", "", "Imports a key encrypted under a ZMK.")]
    public class ImportKey_A6 : AHostCommand
    {
        private string _keyType = "";
        private string _zmk = "";
        private string _key = "";
        private string _keySchemeLMK = "";
        private string _atallaVariant = "";

        public ImportKey_A6()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            string ret = string.Empty;
            ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            if (XMLParseResult == ErrorCodes.ER_00_NO_ERROR)
            {
                _keyType = kvp.Item("Key Type");
                _zmk = kvp.Item("ZMK");
                _key = kvp.Item("Key");
                _keySchemeLMK = kvp.ItemOptional("Key Scheme LMK");
                _atallaVariant = kvp.ItemOptional("Atalla Variant");
            }
            XMLParseResult = ret;
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();

            LMKPairs.LMKPair LMKKeyPair;
            string var = "";
            KeySchemeTable.KeyScheme ks = KeySchemeTable.KeyScheme.Unspecified;

            // validate key type
            if (!ValidateKeyTypeCode(_keyType, out LMKKeyPair, ref var, ref mr)) return mr;

            // determine ZMK scheme from supplied ZMK
            HexKey zmkHK = new HexKey(_zmk);
            KeySchemeTable.KeyScheme zmkKS = zmkHK.Scheme;

            // determine encrypted key scheme/length
            HexKey encKeyHK = new HexKey(_key);
            KeySchemeTable.KeyScheme encKeyKS = encKeyHK.Scheme;

            // encrypted key must be ANSI, not variant
            if ((encKeyKS == KeySchemeTable.KeyScheme.DoubleLengthKeyVariant) || (encKeyKS == KeySchemeTable.KeyScheme.TripleLengthKeyVariant))
            {
                mr.AddElement(ErrorCodes.ER_05_INVALID_KEY_LENGTH_FLAG);
                return mr;
            }

            // decrypt ZMK under LMK
            string clearZMK = Utility.DecryptZMKEncryptedUnderLMK(new HexKey(_zmk).ToString(), zmkKS, 0);

            if (!Utility.IsParityOK(clearZMK, Utility.ParityCheck.OddParity))
            {
                mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                return mr;
            }

            // decrypt the supplied key using clear ZMK
            string clearKey = TripleDES.TripleDESDecrypt(new HexKey(clearZMK), new HexKey(_key).ToString());

            // target LMK key scheme
            KeySchemeTable.KeyScheme targetKS = KeySchemeTable.KeyScheme.Unspecified;
            if (!ValidateKeySchemeCode(_keySchemeLMK, ref targetKS, ref mr)) return mr;

            // encrypt under LMK
            string cryptUnderLMK = Utility.EncryptUnderLMK(clearKey, targetKS, LMKKeyPair, var);

            string chkVal = TripleDES.TripleDESEncrypt(new HexKey(clearKey), Constants.ZEROES);

            Log.Logger.MinorInfo("Imported key (clear): " + clearKey);
            Log.Logger.MinorInfo("Imported key (LMK): " + cryptUnderLMK);
            Log.Logger.MinorInfo("Check value: " + chkVal.Substring(0, 6));

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(cryptUnderLMK);
            mr.AddElement(chkVal.Substring(0, 6));

            return mr;
        }
    }
}
