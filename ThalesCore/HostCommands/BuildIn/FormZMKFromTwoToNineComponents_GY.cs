using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;
using ThalesCore.Cryptography;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("GY", "GZ", "", "Forms a ZMK from 2 to 9 components")]
    public class FormZMKFromTwoToNineComponents_GY : AHostCommand
    {
        public FormZMKFromTwoToNineComponents_GY()
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

            // collect 2..9 components
            var comps = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 9; i++)
            {
                string name = (i == 0) ? "ZMK Component" : string.Format("ZMK Component #{0}", i);
                if (kvp.ContainsKey(name))
                    comps.Add(kvp.Item(name));
                else
                    break;
            }

            if (comps.Count < 2 || comps.Count > 9)
            {
                mr.AddElement(ErrorCodes.ER_80_DATA_LENGTH_ERROR);
                return mr;
            }

            string[] clearComponents = new string[comps.Count];

            // Validate first component scheme/parity and decrypt all components
            for (int i = 0; i < comps.Count; i++)
            {
                string comp = comps[i];

                if (i == 0)
                {
                    try
                    {
                        var hk = new HexKey(comp);
                    }
                    catch (Exception)
                    {
                        mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                        return mr;
                    }
                }

                try
                {
                    KeySchemeTable.KeyScheme ks = new HexKey(comp).Scheme;
                    string compNoPrefix = new HexKey(comp).ToString();
                    clearComponents[i] = Utility.DecryptZMKEncryptedUnderLMK(compNoPrefix, ks, 0);
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

            // Determine scheme from first component for encryption under LMK
            KeySchemeTable.KeyScheme scheme = new HexKey(comps[0]).Scheme;

            // Encrypt final ZMK under LMK pair 04_05 and compute KCV
            string cryptKey = Utility.EncryptUnderLMK(finalKey, scheme, LMKPairs.LMKPair.Pair04_05, "0");
            string chkVal = TripleDES.TripleDESEncrypt(new HexKey(finalKey), ThalesCore.HostCommands.Constants.ZEROES);

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(cryptKey);
            mr.AddElement(chkVal.Substring(0, 6));

            return mr;
        }
    }
}
