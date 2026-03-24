using HostCommands;
using System;
using ThalesCore.Cryptography;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("A8", "A9", "", "Exports a key under a ZMK.")]
    public class ExportKey_A8 : AHostCommand
    {
        private string _encryptedKey = "";
        private string _encryptedZMK = "";
        private string _keyScheme = "";
        private string _keyType = "";

        public ExportKey_A8()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            string ret = string.Empty;
            ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            if (XMLParseResult == ErrorCodes.ER_00_NO_ERROR)
            {
                _encryptedKey = kvp.Item("Key");
                _encryptedZMK = kvp.Item("ZMK");
                _keyScheme = kvp.ItemOptional("Key Scheme");
                _keyType = kvp.Item("Key Type");
            }
            XMLParseResult = ret;
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();

            LMKPairs.LMKPair LMKKeyPair;
            string var = "";
            KeySchemeTable.KeyScheme ks = KeySchemeTable.KeyScheme.Unspecified;

            if (!ValidateKeyTypeCode(_keyType, out LMKKeyPair, ref var, ref mr)) return mr;

            HexKey.KeyLength kl;
            KeySchemeTable.KeyScheme encKeyKS;
            ExtractKeySchemeAndLength(_encryptedKey, out kl, out encKeyKS);

            HexKey.KeyLength zmkKL;
            KeySchemeTable.KeyScheme zmkKS;
            ExtractKeySchemeAndLength(_encryptedZMK, out zmkKL, out zmkKS);

            string clearZMK = Utility.DecryptZMKEncryptedUnderLMK(new HexKey(_encryptedZMK).ToString(), zmkKS, 0);
            string clearKey = Utility.DecryptUnderLMK(new HexKey(_encryptedKey).ToString(), encKeyKS, LMKKeyPair, var);

            KeySchemeTable.KeyScheme targetKS = KeySchemeTable.GetKeySchemeFromValue(_keyScheme);

            string cryptUnderZMK = Utility.EncryptUnderZMK(clearZMK, new HexKey(clearKey).ToString(), targetKS);
            string chkVal = TripleDES.TripleDESEncrypt(new HexKey(clearKey), Constants.ZEROES);

            Log.Logger.MinorInfo("Key (clear): " + clearKey);
            Log.Logger.MinorInfo("Key (ZMK): " + cryptUnderZMK);

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(cryptUnderZMK);
            mr.AddElement(chkVal.Substring(0, 6));

            return mr;
        }

        private void ExtractKeySchemeAndLength(string key, out HexKey.KeyLength keyLen, out KeySchemeTable.KeyScheme keyScheme)
        {
            HexKey hk = new HexKey(key);
            keyLen = hk.KeyLen;
            keyScheme = hk.Scheme;
        }
    }
}
