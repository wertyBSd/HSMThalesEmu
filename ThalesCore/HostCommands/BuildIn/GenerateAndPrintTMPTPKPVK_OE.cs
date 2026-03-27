using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("OE", "OF", "", "Generates a random key TMK, TPK or PVK and prints it in the clear.")]
    public class GenerateAndPrintTMPTPKPVK_OE : AHostCommand
    {
        public GenerateAndPrintTMPTPKPVK_OE()
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

            string keyType = kvp.ContainsKey("Key Type") ? kvp.Item("Key Type") : "";

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

            string component;
            try
            {
                component = Utility.CreateRandomKey(ks);
            }
            catch (Exception)
            {
                mr.AddElement(ErrorCodes.ER_ZZ_UNKNOWN_ERROR);
                return mr;
            }

            ClearPrinterData();
            if (!string.IsNullOrEmpty(keyType)) AddPrinterData("Key Type: " + keyType);
            AddPrinterData("Component: " + component);

            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(component);
            return mr;
        }
    }
}
