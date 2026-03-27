using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Cryptography;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("BI", "BJ", "", "Generates a random BDK.")]
    public class GenerateBDK_BI : AHostCommand
    {
        public GenerateBDK_BI()
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

            // Key Scheme must be present and valid
            if (!kvp.ContainsKey("Key Scheme LMK"))
            {
                mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME);
                return mr;
            }

            string schemeChar = kvp.Item("Key Scheme LMK");
            ThalesCore.KeySchemeTable.KeyScheme ks;
            try
            {
                ks = ThalesCore.KeySchemeTable.GetKeySchemeFromValue(schemeChar);
            }
            catch (Exception)
            {
                mr.AddElement(ErrorCodes.ER_26_INVALID_KEY_SCHEME);
                return mr;
            }

            // Use LMK pair 04_05 variant 0 for BDK generation
            LMKPairs.LMKPair LMKKeyPair = LMKPairs.LMKPair.Pair04_05;
            string var = "0";

            if (!ValidateFunctionRequirement(KeyTypeTable.KeyFunction.Generate, LMKKeyPair, var, mr))
                return mr;

            string bdkClear;
            try
            {
                bdkClear = Utility.CreateRandomKey(ks);
            }
            catch (Exception)
            {
                mr.AddElement(ErrorCodes.ER_ZZ_UNKNOWN_ERROR);
                return mr;
            }

            string cryptBDK = Utility.EncryptUnderLMK(bdkClear, ks, LMKKeyPair, var);
            string chkVal = TripleDES.TripleDESEncrypt(new HexKey(bdkClear), Constants.ZEROES);

            Log.Logger.MinorInfo("BDK generated (clear): " + bdkClear);
            Log.Logger.MinorInfo("BDK generated (LMK): " + cryptBDK);
            Log.Logger.MinorInfo("Check value: " + chkVal.Substring(0, 6));

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(cryptBDK);
            mr.AddElement(chkVal.Substring(0, 6));

            return mr;
        }
    }
}
