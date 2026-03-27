using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;
using ThalesCore.Storage;
using System.Text;
using System.Security.Cryptography;
using ThalesCore.PIN;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("CA", "CB", "", "Translate PIN From TPK to ZPK")]
    public class TranslatePINFromTPKToZPK_CA : AHostCommand
    {
        private string _rawMessage = string.Empty;

        public TranslatePINFromTPKToZPK_CA()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            string ret = string.Empty;
            ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            _rawMessage = msg?.MessageData ?? string.Empty;
            XMLParseResult = ret;
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();
            try
            {
                string sourceTpk = kvp.Item("Source TPK");
                string destZpk = kvp.Item("Destination ZPK");
                string pinLenStr = kvp.ItemOptional("PIN Length") ?? kvp.ItemOptional("Max PIN Length") ?? "12";
                string srcPinBlock = kvp.Item("Source PIN Block");
                string srcFormat = kvp.ItemOptional("Source PIN Block Format Code") ?? kvp.ItemOptional("Source PIN Block Format") ?? "01";
                string dstFormat = kvp.ItemOptional("Target PIN Block Format Code") ?? kvp.ItemOptional("Target PIN Block Format") ?? kvp.ItemOptional("Destination PIN Block Format Code") ?? "01";
                string account = kvp.ItemOptional("Account Number") ?? string.Empty;
                // preserve raw message fallback for PAN extraction
                string rawMsg = _rawMessage ?? string.Empty;

                var srcFmt = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat(srcFormat);
                var dstFmt = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat(dstFormat);

                // Decrypt source PIN block under source TPK
                // Some implementations may include non-hex characters around key blobs; ensure we extract contiguous hex if needed
                string srcTpkHex = sourceTpk;
                if (!System.Text.RegularExpressions.Regex.IsMatch(srcTpkHex, "^[0-9A-Fa-f]+$") && !string.IsNullOrEmpty(rawMsg))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(rawMsg, "([0-9A-Fa-f]{16,32})");
                    if (m.Success) srcTpkHex = m.Groups[1].Value;
                }

                string decBlock = ThalesCore.Cryptography.TripleDES.TripleDESDecrypt(new ThalesCore.Cryptography.HexKey(srcTpkHex), srcPinBlock);

                // Extract clear PIN
                string clearPIN = ThalesCore.PIN.PINBlockFormat.ToPIN(decBlock, account, srcFmt);

                int maxPinLen = Convert.ToInt32(pinLenStr);
                if ((clearPIN.Length < 4) || (clearPIN.Length > 12) || (clearPIN.Length > maxPinLen))
                {
                    mr.AddElement(ErrorCodes.ER_24_PIN_IS_FEWER_THAN_4_OR_MORE_THAN_12_DIGITS_LONG);
                    return mr;
                }

                // Create destination PIN block
                string dstBlock = ThalesCore.PIN.PINBlockFormat.ToPINBlock(clearPIN, account, dstFmt);

                // Encrypt destination block under destination ZPK
                string cryptDst = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(destZpk), dstBlock);

                    mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                    mr.AddElement(cryptDst);

                    // Best-effort: persist PVV + IBM3624 offset for this account under destination ZPK
                    try
                    {
                        var store = StoreFactory.CreateFromEnvironment();
                        store.InitializeAsync().GetAwaiter().GetResult();

                        // If parsed account is not numeric, attempt to find PAN in the raw message
                        string panForPvv = account;
                        if (string.IsNullOrEmpty(panForPvv) || !System.Text.RegularExpressions.Regex.IsMatch(panForPvv, "^[0-9]{12,19}$"))
                        {
                            var m2 = System.Text.RegularExpressions.Regex.Match(rawMsg ?? string.Empty, "([0-9]{12,19})");
                            if (m2.Success) panForPvv = m2.Groups[1].Value;
                        }

                        // Compute Visa PVV (4 digits) and IBM 3624 offset
                        var pvv = PVV.ComputeVisaPVV(destZpk, panForPvv);
                        var offset = PVV.ComputeIBM3624Offset(destZpk, panForPvv, clearPIN);

                        var storeType = Environment.GetEnvironmentVariable("THALES_STORE")?.ToLowerInvariant() ?? "json";
                        string encPvvB64;
                        string encOffsetB64;
                        if (storeType == "json")
                        {
                            encPvvB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(pvv));
                            encOffsetB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(offset));
                        }
                        else
                        {
                            var protectedPvv = ProtectedData.Protect(Encoding.UTF8.GetBytes(pvv), null, DataProtectionScope.CurrentUser);
                            encPvvB64 = Convert.ToBase64String(protectedPvv);
                            var protectedOffset = ProtectedData.Protect(Encoding.UTF8.GetBytes(offset), null, DataProtectionScope.CurrentUser);
                            encOffsetB64 = Convert.ToBase64String(protectedOffset);
                        }

                        var acctId = panForPvv;
                        var accRec = new ThalesCore.Storage.AccountRecord(acctId, panForPvv, encPvvB64, encOffsetB64, 0, 0);
                        store.CreateOrUpdateAccountAsync(accRec).GetAwaiter().GetResult();

                        // Try to read back and log for diagnostics
                        var read = store.GetAccountAsync(panForPvv).GetAwaiter().GetResult();
                        if (read == null)
                        {
                            Log.Logger.MinorInfo("TranslatePIN CA: persisted account not found after write: " + panForPvv);
                        }
                        else
                        {
                            Log.Logger.MinorInfo("TranslatePIN CA: persisted account OK for " + panForPvv);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.MinorInfo("TranslatePIN CA: failed to persist PVV/offset to store: " + ex.Message);
                        Console.WriteLine("TranslatePIN CA: outer exception occurred " + ex?.ToString());
                    }

                    return mr;
            }
            catch (ThalesCore.Exceptions.XUnsupportedPINBlockFormat)
            {
                mr.AddElement(ErrorCodes.ER_23_INVALID_PIN_BLOCK_FORMAT_CODE);
                return mr;
            }
            catch (ThalesCore.Exceptions.XInvalidAccount)
            {
                mr.AddElement(ErrorCodes.ER_20_PIN_BLOCK_DOES_NOT_CONTAIN_VALID_VALUES);
                return mr;
            }
            catch (Exception)
            {
                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                return mr;
            }
        }
    }
}
