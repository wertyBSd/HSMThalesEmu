using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;
using ThalesCore.Cryptography;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("DG", "DH", "", "Generates a 4-digit VISA PVV.")]
    public class GenerateVISAPVV_DG : AHostCommand
    {
        public GenerateVISAPVV_DG()
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
                // PVK may be provided as multi-format (scheme + key)
                string pvkField = kvp.ItemCombination("PVK Scheme", "PVK").Trim();

                if (String.IsNullOrEmpty(pvkField))
                {
                    mr.AddElement(ErrorCodes.ER_15_INVALID_INPUT_DATA);
                    return mr;
                }

                HexKey pvk = null;
                bool parsed = false;
                try
                {
                    pvk = new HexKey(pvkField);
                    parsed = true;
                }
                catch (ThalesCore.Exceptions.XInvalidKeyScheme)
                {
                    // fallback
                }
                catch (ThalesCore.Exceptions.XInvalidKey)
                {
                    // fallback
                }

                if (!parsed && pvkField.Length > 1 && pvkField[0] == pvkField[1])
                {
                    string alt = pvkField.Substring(1);
                    try
                    {
                        pvk = new HexKey(alt);
                        parsed = true;
                    }
                    catch { }
                }

                if (!parsed)
                {
                    try
                    {
                        var _ = new HexKey(pvkField);
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

                string clearPVK = pvk.ToString();

                if (!Utility.IsParityOK(clearPVK, Utility.ParityCheck.OddParity))
                {
                    mr.AddElement(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR);
                    return mr;
                }

                string pin = kvp.ItemOptional("PIN");
                string acct = kvp.ItemOptional("Account Number");
                string pvki = kvp.ItemOptional("PVKI");

                // Build PVKPair (include scheme prefix if present)
                string pvkPair = KeySchemeTable.GetKeySchemeValue(pvk.Scheme) + clearPVK;

                string pvv = GeneratePVV(acct, pvki, pin, pvkPair);

                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                mr.AddElement(pvv);
                return mr;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GenerateVISAPVV_DG Exception: " + ex.ToString());
                mr.AddElement(ErrorCodes.ER_ZZ_UNKNOWN_ERROR);
                return mr;
            }
        }
    }
}
