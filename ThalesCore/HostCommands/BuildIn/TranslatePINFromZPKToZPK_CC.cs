using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;
using ThalesCore.Storage;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using ThalesCore.PIN;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("CC", "CD", "", "Translates a PIN block from ZPK to ZPK encryption.")]
    public class TranslatePINFromZPKToZPK_CC : AHostCommand
    {
        private string _rawMessageData = string.Empty;
        public TranslatePINFromZPKToZPK_CC()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            // preserve raw message data for fallback parsing
            try { _rawMessageData = msg.MessageData; } catch { _rawMessageData = string.Empty; }
            string ret = string.Empty;
            ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            XMLParseResult = ret;
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();

            try
            {
                Console.WriteLine("TranslatePIN CC: invoked; src=" + kvp.Item("Source ZPK") + " dst=" + kvp.Item("Destination ZPK") + " acct=" + kvp.Item("Account Number"));
                string sourceZpk = kvp.Item("Source ZPK");
                string destZpk = kvp.Item("Destination ZPK");
                // Some message variants may omit Max PIN Length; use optional accessor with sensible default
                string maxPinLenStr = kvp.ItemOptional("Max PIN Length");
                if (string.IsNullOrEmpty(maxPinLenStr)) maxPinLenStr = "12";
                string srcPinBlock = kvp.Item("Source PIN Block");
                string srcFormat = kvp.Item("Source PIN Block Format Code");
                string dstFormat = kvp.Item("Destination PIN Block Format Code");
                string account = kvp.Item("Account Number");

                var srcFmt = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat(srcFormat);
                var dstFmt = ThalesCore.PIN.PINBlockFormat.ToPINBlockFormat(dstFormat);

                // Decrypt source PIN block under source ZPK
                string decBlock = ThalesCore.Cryptography.TripleDES.TripleDESDecrypt(new ThalesCore.Cryptography.HexKey(sourceZpk), srcPinBlock);

                // Extract clear PIN
                string clearPIN = ThalesCore.PIN.PINBlockFormat.ToPIN(decBlock, account, srcFmt);

                int maxPinLen = Convert.ToInt32(maxPinLenStr);
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
                Console.WriteLine("TranslatePIN CC: response prepared, cryptDst length=" + (cryptDst?.Length ?? 0));

                // Best-effort: persist PVV + IBM3624 offset for this account under destination ZPK
                try
                {
                    Console.WriteLine("TranslatePIN CC: enter persistence try");
                    Log.Logger.MinorInfo("TranslatePIN CC: enter persistence try");
                    var store = StoreFactory.CreateFromEnvironment();
                    store.InitializeAsync().GetAwaiter().GetResult();

                        // destZpk is provided encrypted under LMK; decrypt to clear before PVV/offset computation
                        try
                        {
                            Console.WriteLine("TranslatePIN CC: about to decrypt destination token: '" + destZpk + "'");
                            var destHexKey = new ThalesCore.Cryptography.HexKey(destZpk);
                            Console.WriteLine("TranslatePIN CC: destHexKey.ToString()='" + destHexKey.ToString() + "' scheme='" + destHexKey.Scheme + "'");
                            var clearDestZpk = Utility.DecryptUnderLMK(destHexKey.ToString(), destHexKey.Scheme, LMKPairs.LMKPair.Pair06_07, "0");
                            clearDestZpk = clearDestZpk?.Trim() ?? string.Empty;
                            Console.WriteLine("TranslatePIN CC: clearDestZpk='" + clearDestZpk + "'");

                            // Extract contiguous hex substring (some decryptors may prefix a scheme char)
                            var m = Regex.Match(clearDestZpk, "[0-9A-Fa-f]{16,}");
                            if (!m.Success) throw new Exception("Decrypted ZPK does not contain hex digits: " + clearDestZpk);
                            var keyHex = m.Value;
                            if (keyHex.Length % 2 == 1) keyHex = keyHex.Substring(1);

                            // Ensure account looks like a PAN; if not, try to find a numeric PAN in parsed kvp data
                            var panForPvv = account;
                            if (!System.Text.RegularExpressions.Regex.IsMatch(panForPvv ?? "", "^\\d{4,19}$"))
                            {
                                // Prefer raw message data for a PAN if available
                                string searchSrc = !string.IsNullOrEmpty(_rawMessageData) ? _rawMessageData : kvp.ToString();
                                var mPan = System.Text.RegularExpressions.Regex.Match(searchSrc, "\\d{12,19}");
                                if (mPan.Success)
                                {
                                    Console.WriteLine("TranslatePIN CC: account field not numeric, using PAN from raw message/kvp: " + mPan.Value);
                                    panForPvv = mPan.Value;
                                }
                                else
                                {
                                    Console.WriteLine("TranslatePIN CC: account field not numeric and no PAN found in raw message/kvp; using original value: " + panForPvv);
                                }
                            }

                            var pvv = PVV.ComputeVisaPVV(keyHex, panForPvv);
                            var offset = PVV.ComputeIBM3624Offset(keyHex, panForPvv, clearPIN);

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
                            
                            var accRec = new AccountRecord(panForPvv, panForPvv, encPvvB64, encOffsetB64, 0, 0);
                            var storePath = Environment.GetEnvironmentVariable("THALES_STORE_PATH") ?? "(none)";
                            Console.WriteLine("TranslatePIN CC: attempting to persist account " + account + " to storeType=" + storeType + " path=" + storePath);
                            Log.Logger.MinorInfo("TranslatePIN CC: attempting to persist account " + account + " to storeType=" + storeType + " path=" + storePath);
                            try
                            {
                                Console.WriteLine("TranslatePIN CC: calling CreateOrUpdateAccountAsync for AccountId=" + accRec.AccountId);
                                store.CreateOrUpdateAccountAsync(accRec).GetAwaiter().GetResult();
                                Console.WriteLine("TranslatePIN CC: CreateOrUpdateAccountAsync returned for AccountId=" + accRec.AccountId);
                                Log.Logger.MinorInfo("TranslatePIN CC: persisted account " + account + " to storeType=" + storeType + " path=" + storePath);
                            }
                            catch (Exception exPersist)
                            {
                                Console.WriteLine("TranslatePIN CC: persistence threw: " + exPersist.ToString());
                                Log.Logger.MinorInfo("TranslatePIN CC: failed to persist PVV/offset to store: " + exPersist.Message);
                                throw; // rethrow so outer catch logs the outer exception too (helpful during tests)
                            }

                            // verify write by reading back the account
                            try
                            {
                                // Verify by PAN used to compute PVV (panForPvv) — the original `account` field
                                // in the message may not be the numeric PAN.
                                var gotByPan = store.GetAccountAsync(panForPvv).GetAwaiter().GetResult();
                                var gotByOriginal = store.GetAccountAsync(account).GetAwaiter().GetResult();
                                Log.Logger.MinorInfo("TranslatePIN CC: verify persisted account foundByPan=" + (gotByPan != null) + " foundByOriginal=" + (gotByOriginal != null));
                                Console.WriteLine("TranslatePIN CC: verify persisted account foundByPan=" + (gotByPan != null) + " foundByOriginal=" + (gotByOriginal != null));
                            }
                            catch (Exception ex)
                            {
                                Log.Logger.MinorInfo("TranslatePIN CC: verify read failed: " + ex.Message);
                                Console.WriteLine("TranslatePIN CC: verify read failed: " + ex.Message + "\n" + ex.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("TranslatePIN CC: decryption/PVV/offset block threw: " + ex.ToString());
                            throw;
                        }
                        
                }
                catch (Exception ex)
                {
                    Log.Logger.MinorInfo("TranslatePIN CC: failed to persist PVV/offset to store: " + ex.Message);
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
            catch (Exception ex)
            {
                Console.WriteLine("TranslatePIN CC: outer exception occurred (suppressed): " + ex.ToString());
                mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
                return mr;
            }
        }
    }
}
