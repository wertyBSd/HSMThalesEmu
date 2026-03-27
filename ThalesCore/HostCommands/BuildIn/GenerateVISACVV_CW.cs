using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;
using ThalesCore.Cryptography;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("CW", "CX", "", "Generates a VISA CVV.")]
    public class GenerateVISACVV_CW : AHostCommand
    {
        public GenerateVISACVV_CW()
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

            try
            {
                string del = kvp.ItemOptional("Delimiter");

                // CVK may be provided as multi-format (scheme + key) via fields 'CVK Scheme' and 'CVK'
                string cvkField = kvp.ItemCombination("CVK Scheme", "CVK").Trim();

                if (String.IsNullOrEmpty(cvkField))
                {
                    mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                    return mr;
                }

                HexKey cvk = null;
                bool parsed = false;
                try
                {
                    cvk = new HexKey(cvkField);
                    parsed = true;
                }
                catch (ThalesCore.Exceptions.XInvalidKeyScheme)
                {
                    // try fallback below
                }
                catch (ThalesCore.Exceptions.XInvalidKey)
                {
                    // try fallback below
                }

                // Fallback: duplicated scheme char (ItemCombination may duplicate scheme)
                if (!parsed && cvkField.Length > 1 && cvkField[0] == cvkField[1])
                {
                    string alt = cvkField.Substring(1);
                    try
                    {
                        cvk = new HexKey(alt);
                        parsed = true;
                    }
                    catch
                    {
                        // fall through
                    }
                }

                if (!parsed)
                {
                    try
                    {
                        var _ = new HexKey(cvkField);
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

                    mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                    return mr;
                }

                string clearCVK = cvk.ToString();

                if (!Utility.IsParityOK(clearCVK, Utility.ParityCheck.OddParity))
                {
                    mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                    return mr;
                }

                string pan = kvp.ItemOptional("Primary Account Number");
                string exp = kvp.ItemOptional("Expiration Date");
                string svc = kvp.ItemOptional("Service Code");

                // Build CVKPair for GenerateCVV (include key type prefix)
                string cvkPair = KeySchemeTable.GetKeySchemeValue(cvk.Scheme) + clearCVK;

                string cvv = GenerateCVV(cvkPair, pan, exp, svc);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(cvv);
                return mr;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GenerateVISACVV_CW Exception: " + ex.ToString());
                mr.AddElement(ErrorCodes.ER_ZZ_UNKNOWN_ERROR);
                return mr;
            }
        }
    }
}
