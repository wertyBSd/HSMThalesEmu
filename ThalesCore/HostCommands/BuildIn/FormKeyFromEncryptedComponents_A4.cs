using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;
using ThalesCore.Cryptography;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("A4", "A5", "", "Forms a key from encrypted components")]
    public class FormKeyFromEncryptedComponents_A4 : AHostCommand
    {
        private string _numberOfComponents = "";
        private string _keyType = "";
        private string _keyScheme = "";

        public FormKeyFromEncryptedComponents_A4()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            string ret = string.Empty;
            ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            if (ret == ErrorCodes.ER_00_NO_ERROR)
            {
                _numberOfComponents = kvp.Item("Number of Components");
                _keyType = kvp.Item("Key Type");
                _keyScheme = kvp.ItemOptional("Key Scheme (LMK)");
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

            if (!ValidateKeySchemeCode(_keyScheme, ref ks, ref mr)) return mr;

            if (!ValidateFunctionRequirement(KeyTypeTable.KeyFunction.Generate, LMKKeyPair, var, mr)) return mr;

            int num = 0;
            try { num = Convert.ToInt32(_numberOfComponents); }
            catch { mr.AddElement(ErrorCodes.ER_03_INVALID_NUMBER_OF_COMPONENTS); return mr; }

            string[] clearComponents = new string[num];

            for (int i = 0; i < num; i++)
            {
                string keyName = (i == 0) ? "Key Component" : string.Format("Key Component #{0}", i);
                if (!kvp.ContainsKey(keyName))
                {
                    mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                    return mr;
                }

                string comp = kvp.Item(keyName);

                // Remove any key-scheme prefix from component and decrypt under LMK
                string removeType = Utility.RemoveKeyType(comp);
                try
                {
                    clearComponents[i] = Utility.DecryptUnderLMK(removeType, ks, LMKKeyPair, var);
                }
                catch (Exception)
                {
                    mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                    return mr;
                }
            }

            // XOR all clear components
            string xorred = clearComponents[0];
            for (int i = 1; i < clearComponents.Length; i++)
                xorred = Utility.XORHexStringsFull(xorred, clearComponents[i]);

            // Ensure odd parity on result
            string finalKey = Utility.MakeParity(xorred, Utility.ParityCheck.OddParity);

            // Encrypt final key under LMK and calculate KCV
            string cryptKey = Utility.EncryptUnderLMK(finalKey, ks, LMKKeyPair, var);
            string chkVal = TripleDES.TripleDESEncrypt(new HexKey(finalKey), Constants.ZEROES);

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(cryptKey);
            mr.AddElement(chkVal.Substring(0, 6));

            return mr;
        }
    }
}
