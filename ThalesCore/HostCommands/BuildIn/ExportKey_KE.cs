using HostCommands;
using System;
using ThalesCore.Cryptography;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("KE", "KF", "", "Exports a key under a ZMK (host KE).")]
    public class ExportKey_KE : AHostCommand
    {
        private string _keyType = "";
        private string _zmk = "";
        private string _key = "";
        private string _keyScheme = "";

        public ExportKey_KE()
        {
            // reuse XML definition from ExportKey_A8
            ReadXMLDefinitions("ExportKey_A8.xml");
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
                _keyScheme = kvp.ItemOptional("Key Scheme ZMK");
            }
            XMLParseResult = ret;
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();

            LMKPairs.LMKPair LMKKeyPair;
            string var = "";

            if (!ValidateKeyTypeCode(_keyType, out LMKKeyPair, ref var, ref mr)) return mr;

            HexKey.KeyLength kl;
            KeySchemeTable.KeyScheme encKeyKS;
            ExtractKeySchemeAndLength(_key, out kl, out encKeyKS);

            HexKey.KeyLength zmkKL;
            KeySchemeTable.KeyScheme zmkKS;
            ExtractKeySchemeAndLength(_zmk, out zmkKL, out zmkKS);

            string clearZMK = Utility.DecryptZMKEncryptedUnderLMK(new HexKey(_zmk).ToString(), zmkKS, 0);
            string clearKey = Utility.DecryptUnderLMK(new HexKey(_key).ToString(), encKeyKS, LMKKeyPair, var);

            KeySchemeTable.KeyScheme targetKS = KeySchemeTable.KeyScheme.SingleDESKey;
            if (!string.IsNullOrEmpty(_keyScheme))
                targetKS = KeySchemeTable.GetKeySchemeFromValue(_keyScheme);

            string cryptUnderZMK = Utility.EncryptUnderZMK(clearZMK, new HexKey(clearKey).ToString(), targetKS);
            string chkVal = TripleDES.TripleDESEncrypt(new HexKey(clearKey), Constants.ZEROES);

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
