using HostCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThalesCore.Cryptography;
using ThalesCore.Message;
using ThalesCore.HostCommands;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("A0", "A1", "", "Generates and encrypts key under ZMK for transmission")]
    public class GenerateKey_A0 : AHostCommand
    {
        private string _modeFlag = "";
        private string _keyType = "";
        private string _keyScheme = "";
        private string _zmk = "";
        private string _zmkScheme = "";

        public GenerateKey_A0()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(Message.Message msg)
        {
            string ret = "";
            MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            if (XMLParseResult == ErrorCodes.ER_00_NO_ERROR)
            {
                _modeFlag = kvp.Item("Mode");
                _keyType = kvp.Item("Key Type");
                _keyScheme = kvp.Item("Key Scheme LMK");
                _zmkScheme = kvp.ItemOptional("Key Scheme ZMK");
                _zmk = kvp.ItemOptional("ZMK Scheme") + kvp.ItemOptional("ZMK");
            }
            XMLParseResult = ret;
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();

            LMKPairs.LMKPair LMKKeyPair;
            string var = "";
            KeySchemeTable.KeyScheme ks = KeySchemeTable.KeyScheme.Unspecified;
            KeySchemeTable.KeyScheme zmk_ks = KeySchemeTable.KeyScheme.Unspecified;

            if (!ValidateKeyTypeCode(_keyType, out LMKKeyPair, ref var, ref mr)) return mr;

            if(!ValidateKeySchemeCode(_keyScheme, ref ks, ref mr)) return mr;

            if (_zmkScheme != "")
                if (!ValidateKeySchemeCode(_zmkScheme,ref zmk_ks, ref mr)) return mr;

            if(!ValidateFunctionRequirement(KeyTypeTable.KeyFunction.Generate, LMKKeyPair, var, mr))
                return mr;

            string rndKey = Utility.CreateRandomKey(ks);
            string cryptRndKey = Utility.EncryptUnderLMK(rndKey, ks, LMKKeyPair, var);
            string chkVal = TripleDES.TripleDESEncrypt(new HexKey(rndKey), Constants.ZEROES);

            Log.Logger.MinorInfo("Key generated (clear): " + rndKey);
            Log.Logger.MinorInfo("Key generated (LMK): " + cryptRndKey);
            Log.Logger.MinorInfo("Check value: " + chkVal.Substring(0, 6));

            string clearZMK;
            string cryptUnderZMK = "";

            if (_zmk != "")
            {
                HexKey cryptZMK = new HexKey(_zmk);

                clearZMK = Utility.DecryptZMKEncryptedUnderLMK(cryptZMK.ToString(), cryptZMK.Scheme, 0);

                if (!Utility.IsParityOK(clearZMK, Utility.ParityCheck.OddParity))
                {
                    mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                    return mr;
                }

                cryptUnderZMK = Utility.EncryptUnderZMK(clearZMK, rndKey, zmk_ks);

                Log.Logger.MinorInfo("ZMK (clear): " + clearZMK);
                Log.Logger.MinorInfo("Key under ZMK: " + cryptUnderZMK);
            }

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(cryptRndKey);
            mr.AddElement(cryptUnderZMK);
            mr.AddElement(chkVal.Substring(0, 6));

            return mr;
        }
    }
}
