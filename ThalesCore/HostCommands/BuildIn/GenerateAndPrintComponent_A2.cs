using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("A2", "A3", "", "Generate and print a component.")]
    public class GenerateAndPrintComponent_A2 : AHostCommand
    {
        public GenerateAndPrintComponent_A2()
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

            // Key Type is optional for generation here - used for printing only
            string keyType = kvp.ContainsKey("Key Type") ? kvp.Item("Key Type") : "";

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

            // Generate a random component matching the requested key scheme
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

            // Prepare printer output (human-readable)
            ClearPrinterData();
            if (!string.IsNullOrEmpty(keyType)) AddPrinterData("Key Type: " + keyType);
            AddPrinterData("Component: " + component);

            // Return response: 00 + component (component printed in clear)
            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            mr.AddElement(component);
            return mr;
        }
    }
}
