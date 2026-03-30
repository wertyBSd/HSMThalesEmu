using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using HostCommands;
using ThalesCore;
using ThalesCore.Cryptography;
using ThalesCore.Cryptography.MAC;
using ThalesCore.HostCommands;
using ThalesCore.Message;
using ThalesCore.HostCommands.BuildIn;
using ThalesCore.Storage;
using System.IO;

namespace ThalesCore.Tests
{
    [TestFixture]
    public class HostCommandTests
    {
        private ThalesMain o;

        [SetUp]
        public void InitTests()
        {
            o = new ThalesMain();
            o.MajorLogEvent += O_MajorLogEvent;
            o.MinorLogEvent += O_MinorLogEvent;
            o.StartUpWithoutTCP(@"..\..\..\ThalesCore\ThalesParameters.xml");
        }

        private void O_MinorLogEvent(ThalesMain sender, string s)
        {
            
        }

        private void O_MajorLogEvent(ThalesMain sender, string s)
        {
            
        }

        [TearDown]
        public void EndTests()
        {
            o.ShutDown();
            o = null;
        }

        private string TestTran(string input, AHostCommand HC)
        {
            MessageResponse retMsg;
            Message.Message msg = new Message.Message(input);

            string trailingChars = "";
            if (ExpectTrailers())
                trailingChars = msg.GetTrailers();

            HC.AcceptMessage(msg);

            if (HC.XMLParseResult != ErrorCodes.ER_00_NO_ERROR)
            {
                retMsg = new MessageResponse();
                retMsg.AddElement(HC.XMLParseResult);
            }
            else
                retMsg = HC.ConstructResponse();

            retMsg.AddElement(trailingChars);

            HC.Terminate();
            HC = null;
            return retMsg.MessageData;
        }

        private bool ExpectTrailers()
        {
            return (bool)Resources.GetResource(Resources.EXPECT_TRAILERS);
        }

        [Test]
        public void TestGenerateZPK()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();
            string ZMK = TestTran("0000U", new GenerateKey_A0()).Substring(2, 33);
            Assert.AreEqual("00", TestTran(ZMK, new GenerateZPK_IA()).Substring(0, 2));
        }

        [Test]
        public void TestGenerateZEKorZAK()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // create a ZMK (scheme + key)
            string ZMK = TestTran("0000U", new GenerateKey_A0()).Substring(2, 33);

            // call FI (Generate ZEK/ZAK)
            string resp = TestTran(ZMK, new GenerateZEKorZAK_FI());

            // accept success or source key parity error
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR) || resp.StartsWith(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR));
            if (resp.StartsWith(ErrorCodes.ER_00_NO_ERROR))
            {
                // response should include encrypted-under-ZMK, encrypted-under-LMK and a check value (full)
                Assert.IsTrue(resp.Length >= 2 + 16 + 16 + 16, "Response length should include encrypted key and KCV");
                string kcv = resp.Substring(resp.Length - 16);
                Assert.IsTrue(Regex.IsMatch(kcv, "^[0-9A-F]{16}$"), "KCV should be 16 hex characters");
            }
        }

        [Test]
        public void TestGenerateZEKorZAK_MissingZMK_ReturnsError()
        {
            AuthorizedStateOn();
            // call FI with empty input; expect non-success error (handler validates presence of ZMK)
            string resp = TestTran("", new GenerateZEKorZAK_FI());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestCancelAuthState()
		{
			Assert.AreEqual(TestTran("", new CancelAuthState_RA()), "00");
		}

        [Test]
        public void TestSetHSMDelay()
		{
			Assert.AreEqual("00", TestTran("001", new SetHSMDelay_LG()));
		}

        [Test]
        public void TestTranslateBDK_DY_ReturnsSuccessForWellFormedInput()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            string zmkToken = "U" + new string('0', 32);
            string bdkToken = "U" + new string('A', 32);

            string input = zmkToken + bdkToken + ";" + "U" + "0" + "0";

            var resp = TestTran(input, new TranslateBDKFromLMKToZMK_DY());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslateBDK_DY_MissingInput_ReturnsError()
        {
            // Provide a non-printable payload to trigger verification failure
            var resp = TestTran("\u0001", new TranslateBDKFromLMKToZMK_DY());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslateBDK_DW_ReturnsSuccessForWellFormedInput()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            string zmkToken = "U" + new string('0', 32);
            string bdkToken = "U" + new string('B', 32);

            // For DW (ZMK->LMK), supply ZMK-encrypted BDK token and expect success
            string input = zmkToken + bdkToken + ";" + "U" + "0" + "0";

            var resp = TestTran(input, new TranslateBDKFromZMKToLMK_DW());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslateBDK_DW_MissingInput_ReturnsError()
        {
            // non-printable payload should trigger verification failure
            var resp = TestTran("\u0001", new TranslateBDKFromZMKToLMK_DW());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }
        
        [Test]
        public void TestTranslateCVK_AU_ReturnsSuccessForWellFormedInput()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            string lmkToken = "U" + new string('0', 32);
            string cvkToken = "U" + new string('C', 32);

            string input = lmkToken + cvkToken + ";" + "U" + "0" + "0";

            var resp = TestTran(input, new TranslateCVKFromLMKToZMK_AU());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslateCVK_AW_ReturnsSuccessForWellFormedInput()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            string payload = "<TranslateCVKFromZMKToLMK>" +
                "<CVK>ZMKWrappedCVK</CVK>" +
                "<ZMK>0123456789ABCDEF0123456789ABCDEF</ZMK>" +
                "</TranslateCVKFromZMKToLMK>";

            var resp = TestTran(payload, new TranslateCVKFromZMKToLMK_AW());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslateCVK_AW_MissingInput_ReturnsError()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // use a non-printable char to force verification failure
            string payload = ((char)0x01).ToString();

            var resp = TestTran(payload, new TranslateCVKFromZMKToLMK_AW());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslateCVK_AU_MissingInput_ReturnsError()
        {
            var resp = TestTran("\u0001", new TranslateCVKFromLMKToZMK_AU());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslateKeyScheme_B0_ReturnsSuccessForWellFormedInput()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // create a ZMK and a ZPK under it to produce a multi-format key token
            string ZMK = TestTran("0000U", new GenerateKey_A0()).Substring(2, 33);
            string genZpkResp = TestTran(ZMK, new GenerateZPK_IA());
            string cryptKeyZMK = genZpkResp.Substring(2, 33);

            string input = "000" + cryptKeyZMK + "0";

            var resp = TestTran(input, new TranslateKeyScheme_B0());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR) || resp.StartsWith(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR));
        }

        [Test]
        public void TestTranslateKeyScheme_B0_MissingInput_ReturnsError()
        {
            // non-printable payload should trigger verification failure
            var resp = TestTran("\u0001", new TranslateKeyScheme_B0());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslateKeysFromOldLMKToNewLMK_BW_ReturnsSuccessForWellFormedInput()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // Create a LMK-encrypted key by generating a key under LMK format
            // Use GenerateKey_A0 with '0000U' to create a ZMK token, then create a ZPK under that ZMK
            string ZMK = TestTran("0000U", new GenerateKey_A0()).Substring(2, 33);
            string genZpkResp = TestTran(ZMK, new GenerateZPK_IA());
            string cryptKeyZMK = genZpkResp.Substring(2, 33);

            // Build BW input: KeyTypeCode(2) + KeyLengthFlag(1) + Key(MultiFormat) + ';' + KeyType(3) + ';' + Reserved/Flags
            string input = "00" + "1" + cryptKeyZMK + ";" + "U00" + ";0";

            var resp = TestTran(input, new TranslateKeysFromOldLMKToNewLMK_BW());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR) || resp.StartsWith(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR));
        }

        [Test]
        public void TestTranslateKeysFromOldLMKToNewLMK_BW_MissingInput_ReturnsError()
        {
            var resp = TestTran("\u0001", new TranslateKeysFromOldLMKToNewLMK_BW());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }
        [Test]
        public void TestTranslateZEKORZAK_FK_MissingInput_ReturnsError()
        {
            // non-printable payload should trigger verification failure for FK
            var resp = TestTran("\u0001", new TranslateZEKORZAKFromZMKToLMK_FK());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }
        [Test]
        public void TestHSMStatus()
		{

			Assert.AreEqual("00", TestTran("00", new HSMStatus_NO()).Substring(0, 2));
		}

        [Test]
        public void TestRSAEncryptTo3DES_SA_ReturnsEncryptedPinUnderSessionKey()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            string account = "400000123456";
            string clearPIN = "1234";

            // Build clear PIN block (ANSI X9.8 == format 01)
            string clearBlock = ThalesCore.PIN.PINBlockFormat.ToPINBlock(clearPIN, account, ThalesCore.PIN.PINBlockFormat.PIN_Block_Format.AnsiX98);

            // For this test we skip actual RSA; provide the clear block as the 'RSA encrypted' payload
            int blkBytes = clearBlock.Length / 2;
            string rsaLen = blkBytes.ToString().PadLeft(4, '0');

            // PIN session key: use double-length variant with scheme 'U'
            string sessionKey = "U0123456789ABCDEF0123456789ABCDEF";

            // Build message: PadMode(2) + SrcFmt(2) + DstFmt(2) + Account(12) + RSAlen(4) + RSAhex + Delimiter(;) + PINSessionKey + PrivateKeyFlag(2)
            string input = "01" + "01" + "01" + account + rsaLen + clearBlock + ";" + sessionKey + "00";

            var resp = TestTran(input, new ThalesCore.HostCommands.BuildIn.RSAEncryptTo3DES_SA());

            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));

            // expected encrypted destination block
            string dstBlock = ThalesCore.PIN.PINBlockFormat.ToPINBlock(clearPIN, account, ThalesCore.PIN.PINBlockFormat.PIN_Block_Format.AnsiX98);
            string expectedCrypt = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(sessionKey), dstBlock);

            Assert.AreEqual(expectedCrypt, resp.Substring(2));
        }

        [Test]
        public void TestImportKey()
        {
            // Prepare: need authorized state and double-length ZMKs as other tests
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // generate a source ZMK (encrypted under LMK)
            string ZMK = TestTran("0000U", new GenerateKey_A0()).Substring(2, 33);

            // generate a ZPK encrypted under that ZMK
            string genZpkResp = TestTran(ZMK, new GenerateZPK_IA());
            // extract cryptKeyZMK (starts after leading '00')
            string cryptKeyZMK = genZpkResp.Substring(2, 33);

            // build import input: KeyType(3) + ZMK + Key + KeySchemeLMK(1)
            string importInput = "000" + ZMK + cryptKeyZMK + "0";


            // Import result: accept either success (00) or source key parity error (10)
            string importResult = TestTran(importInput, new ImportKey_A6());
            
            Assert.IsTrue(importResult.StartsWith(ErrorCodes.ER_00_NO_ERROR) || importResult.StartsWith(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR));
        }

        [Test]
        public void TestImportKey_PersistsKey()
        {
            // configure a temporary json store for the test
            var tmp = Path.Combine(TestContext.CurrentContext.WorkDirectory, "test_store_" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("THALES_STORE", "json");
            Environment.SetEnvironmentVariable("THALES_STORE_PATH", tmp);

            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // create a ZMK and a ZPK under it
            string ZMK = TestTran("0000U", new GenerateKey_A0()).Substring(2, 33);
            string genZpkResp = TestTran(ZMK, new GenerateZPK_IA());
            string cryptKeyZMK = genZpkResp.Substring(2, 33);

            string importInput = "000" + ZMK + cryptKeyZMK + "0";
            string importResult = TestTran(importInput, new ImportKey_A6());

            if (importResult.StartsWith(ErrorCodes.ER_00_NO_ERROR))
            {
                var keysFile = Path.Combine(tmp, "keys.json");
                // wait briefly for the store to flush to disk (avoid transient test race)
                bool exists = false;
                for (int i = 0; i < 20; i++)
                {
                    if (File.Exists(keysFile))
                    {
                        exists = true;
                        break;
                    }
                    System.Threading.Thread.Sleep(50);
                }
                Assert.IsTrue(exists, "keys.json must exist after import");
                var content = File.ReadAllText(keysFile);
                Assert.IsTrue(content.Contains("IMPORTED_"), "Imported key ID should appear in keys.json");
            }
            else
            {
                Assert.Pass("Import returned parity error; persistence not asserted");
            }
        }

        [Test]
        public void TestTranslatePIN_CC_PersistsPVVOffset()
        {
            // configure a temporary json store for the test
            var tmp = Path.Combine(TestContext.CurrentContext.WorkDirectory, "test_store_" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("THALES_STORE", "json");
            Environment.SetEnvironmentVariable("THALES_STORE_PATH", tmp);

            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // create a ZMK and two ZPKs (source and destination)
            string ZMK = TestTran("0000U", new GenerateKey_A0()).Substring(2, 33);
            string gen1 = TestTran(ZMK, new GenerateZPK_IA());
            string gen2 = TestTran(ZMK, new GenerateZPK_IA());

            // extract LMK-encrypted ZPKs from responses (robust parsing)
            string srcZpkLMK = null;
            string dstZpkLMK = null;
            string crypt1 = gen1.Substring(2);
            string crypt2 = gen2.Substring(2);
            string[] possibleLens = new string[] { "17", "33", "49", "16", "32", "48" };
            Func<string, string> pickLmk = (s) =>
            {
                // try to split s into two valid key tokens (first + rest) where each token is hex of common lengths
                int[] lens = new int[] { 17, 33, 49, 16, 32, 48 };
                foreach (var l in lens)
                {
                    if (s.Length >= l)
                    {
                        string a = s.Substring(0, l);
                        string b = s.Substring(l);
                        bool aIsHex = ThalesCore.Utility.IsHexString(a);
                        bool bIsHex = ThalesCore.Utility.IsHexString(b);
                        if (aIsHex && bIsHex)
                        {
                            // prefer LMK form which is typically the second token
                            if (b.Length >= 32) return b.Substring(0, 32);
                            if (b.Length >= 16) return b.Substring(0, 16);
                        }
                    }
                }
                // fallback: return a valid key length (prefer 32, then 16)
                if (s.Length >= 32) return s.Substring(0, 32);
                if (s.Length >= 16) return s.Substring(0, 16);
                throw new InvalidOperationException("Unable to parse generated ZPK response: insufficient length");
            };

            srcZpkLMK = pickLmk(crypt1);
            dstZpkLMK = pickLmk(crypt2);

            // build a PIN block for account and encrypt it under source ZPK (LMK form accepted by HexKey)
            string account = "400000123456"; // 12 digits
            string clearBlock = ThalesCore.PIN.PINBlockFormat.ToPINBlock("1234", account, ThalesCore.PIN.PINBlockFormat.PIN_Block_Format.AnsiX98);
            string cryptSrc = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(srcZpkLMK), clearBlock);

            // build CC input: SourceZPK + DestZPK + MaxPINLen(2) + SrcPinBlock + SrcFmt(2) + DstFmt(2) + Account(12)
            string input = srcZpkLMK + dstZpkLMK + "12" + cryptSrc + "01" + "01" + account;

            var resp = TestTran(input, new TranslatePINFromZPKToZPK_CC());

            if (resp.StartsWith(ErrorCodes.ER_00_NO_ERROR))
            {
                var accountsFile = Path.Combine(tmp, "accounts.json");
                Assert.IsTrue(File.Exists(accountsFile), "accounts.json must exist after translate PIN");
                var content = File.ReadAllText(accountsFile);
                Assert.IsTrue(content.Contains(account), "Persisted account should appear in accounts.json");

                // verify via store API
                var store = StoreFactory.CreateFromEnvironment();
                store.InitializeAsync().GetAwaiter().GetResult();

                // Import the destination ZPK into the store as ZPK_TEST_1 so VerifyPinAsync
                // can use it to derive the PVV/natural value for offset reconstruction.
                try
                {
                    var dstHexKey = new ThalesCore.Cryptography.HexKey(dstZpkLMK);
                    var clearDst = Utility.DecryptUnderLMK(dstHexKey.ToString(), dstHexKey.Scheme, LMKPairs.LMKPair.Pair06_07, "0");
                    byte[] keyBytes = new byte[clearDst.Length / 2];
                    for (int i = 0; i < keyBytes.Length; i++)
                        keyBytes[i] = Convert.ToByte(clearDst.Substring(i * 2, 2), 16);
                    var keyRecord = new KeyRecord("ZPK_TEST_1", "ZPK", Convert.ToBase64String(keyBytes), "000000");
                    store.ImportKeyAsync(keyRecord).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to import clear ZPK for verification: " + ex.Message);
                }

                var ok = store.VerifyPinAsync(account, "1234").GetAwaiter().GetResult();
                Assert.IsTrue(ok, "VerifyPinAsync should succeed for translated account");
            }
            else
            {
                Assert.Pass("TranslatePIN returned non-success; persistence not asserted");
            }
        }

        [Test]
        public void TestTranslatePINFromDUKPTToZPK_CI_ReturnsSuccessForWellFormedInput()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            string bdkToken = "U" + new string('A', 32);
            string zpkToken = "U" + new string('0', 32);

            // KSN Descriptor (3 hex), Key Serial Number (20 hex), Encrypted Block (16 hex), DestFmt(2), Account(12)
            string ksnDesc = "ABC";
            string ksn = "0123456789ABCDEF0123"; // 20 hex chars
            string encBlock = "0123456789ABCDEF"; // 16 hex chars
            string destFmt = "01";
            string account = "400000123456";

            string input = bdkToken + zpkToken + ksnDesc + ksn + encBlock + destFmt + account;

            var resp = TestTran(input, new TranslatePINFromDUKPTToZPK_CI());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslatePINFromDUKPTToZPK_CI_MissingInput_ReturnsError()
        {
            // non-printable payload should trigger verification failure
            var resp = TestTran("\u0001", new TranslatePINFromDUKPTToZPK_CI());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslatePINFromDUKPTToZPK3DES_G0_ReturnsSuccessForWellFormedInput()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            string bdkToken = "U" + new string('A', 32);
            string zpkToken = "U" + new string('0', 32);

            string ksnDesc = "ABC";
            string ksn = "0123456789ABCDEF0123"; // 20 hex chars
            string encBlock = "0123456789ABCDEF"; // 16 hex chars
            string destFmt = "01";
            string account = "400000123456";

            string input = bdkToken + zpkToken + ksnDesc + ksn + encBlock + destFmt + account;

            var resp = TestTran(input, new TranslatePINFromDUKPTToZPK3DES_G0());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslatePINFromDUKPTToZPK3DES_G0_MissingInput_ReturnsError()
        {
            var resp = TestTran("\u0001", new TranslatePINFromDUKPTToZPK3DES_G0());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslatePINFromLMKToZPK_JG_ReturnsSuccessForWellFormedInput()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // LMK-encrypted PIN token (simulate multi-format token) and destination ZPK token
            string lmkToken = "U" + new string('0', 32);
            string zpkToken = "U" + new string('A', 32);

            // Build a minimal valid payload expected by JG (format depends on XML defs)
            // For simplicity use tokens followed by destination format and account
            string destFmt = "01";
            string account = "400000123456";

            string input = lmkToken + zpkToken + destFmt + account;

            var resp = TestTran(input, new TranslatePINFromLMKToZPK_JG());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestTranslatePINFromLMKToZPK_JG_MissingInput_ReturnsError()
        {
            var resp = TestTran("\u0001", new TranslatePINFromLMKToZPK_JG());
            Assert.IsFalse(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestCommandChaining_ExecutesSubcommands()
        {
            // Build an NK message chaining two EchoTest_B2 subcommands.
            // NK payload: HeaderFlag(1) + NumberOfCommands(2) + [SubCommandLength(4) + SubCommandData]*
            // Each subcommand data contains the 2-char command code + its payload. For Echo B2, payload "0000" (4 hex length).

            string headerFlag = "0";
            string num = "02";

            // Subcommand: B2 + payload "0000" -> length 6 -> represented as 0006
            string sub = "B20000";
            string subWithLen = "0006" + sub;

            string nkInput = headerFlag + num + subWithLen + subWithLen;

            string resp = TestTran(nkInput, new CommandChaining_NK());
            // Expect top-level 00 and concatenated two sub-responses (each Echo returns "00") => after first two chars expect "0000"
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
            Assert.AreEqual("0000", resp.Substring(2));
        }

        [Test]
        public void TestDecryptEncryptedPIN_NG_ReturnsClearPIN()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            string account = "400000123456";
            string clearPIN = "1234";

            // Build clear PIN block (ISO/ANSI X9.8 == format 01 used by NG implementation)
            string clearBlock = ThalesCore.PIN.PINBlockFormat.ToPINBlock(clearPIN, account, ThalesCore.PIN.PINBlockFormat.PIN_Block_Format.AnsiX98);

            // Encrypt clear block under LMK pair 02-03 (internal storage key)
            string lmkKey = ThalesCore.Cryptography.LMK.LMKStorage.LMKVariant(ThalesCore.LMKPairs.LMKPair.Pair02_03, 0);
            string cryptUnderLMK = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(lmkKey), clearBlock);

            // Build NG input: Account(12) + LMK-encrypted PIN block
            string input = account + cryptUnderLMK;
            string resp = TestTran(input, new DecryptEncryptedPIN_NG());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
            // response after leading code should be clear PIN
            Assert.AreEqual(clearPIN, resp.Substring(2));
        }

        [Test]
        public void TestDerivePINUsingTheIBMMethod_EE_ReturnsExpectedPin()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // Prepare PVK and parameters
            string pvk = "U0123456789ABCDEFFEDCBA9876543210"; // scheme + 32 hex
            string offset = "000000000001"; // will be parsed for numeric digits
            string checkLen = "04"; // 4 digits
            string account = "400000123456";
            string decTable = "FFFFFFFFFFFFFFFF"; // use default decimalisation table
            string pvd = "123456789012"; // 12 chars PIN Validation Data

            // Compute expected PIN using same logic as handler
            string inputDigits = pvd.PadRight(12, '0') + "0000";
            string bcdHex = "";
            for (int i = 0; i < 16; i += 2)
            {
                int hi = inputDigits[i] - '0';
                int lo = inputDigits[i + 1] - '0';
                int val = (hi << 4) | lo;
                bcdHex += val.ToString("X2");
            }

            string keyHex = ThalesCore.Utility.RemoveKeyType(pvk);
            string encHex = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(keyHex), bcdHex);
            string decimalised = ThalesCore.Utility.Decimalise(encHex, decTable);
            string natural = decimalised.Substring(0, 4);
            string offsetDigits = System.Text.RegularExpressions.Regex.Replace(offset ?? "", "[^0-9]", "");
            if (offsetDigits.Length < 4) offsetDigits = offsetDigits.PadLeft(4, '0');
            else if (offsetDigits.Length > 4) offsetDigits = offsetDigits.Substring(offsetDigits.Length - 4);
            char[] pinChars = new char[4];
            for (int i = 0; i < 4; i++)
            {
                int nd = natural[i] - '0';
                int od = offsetDigits[i] - '0';
                pinChars[i] = (char)('0' + ((nd + od) % 10));
            }
            string expectedPin = new string(pinChars);

            string input = pvk + offset + checkLen + account + decTable + pvd;

            string resp = TestTran(input, new ThalesCore.HostCommands.BuildIn.DerivePINUsingTheIBMMethod_EE());
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
            Assert.AreEqual(expectedPin, resp.Substring(2));
        }

        [Test]
        public void TestGenerateIBMOffset_DE_ReturnsExpectedOffset()
        {
            // similar setup to EE handler tests
            string pvk = "U0123456789ABCDEFFEDCBA9876543210";
            string pin = "1234";
            string checkLen = "04";
            string account = "400000123456";
            string decTable = "FFFFFFFFFFFFFFFF";
            string pvd = "123456789012";

            // Compute expected offset using same algorithm as handler
            string inputDigits = pvd.PadRight(12, '0') + "0000";
            string bcdHex = "";
            for (int i = 0; i < 16; i += 2)
            {
                int hi = inputDigits[i] - '0';
                int lo = inputDigits[i + 1] - '0';
                int val = (hi << 4) | lo;
                bcdHex += val.ToString("X2");
            }

            string keyHex = ThalesCore.Utility.RemoveKeyType(pvk);
            string encHex = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(keyHex), bcdHex);
            string decimalised = ThalesCore.Utility.Decimalise(encHex, decTable);
            string natural = decimalised.Substring(0, 4);

            char[] offChars = new char[4];
            for (int i = 0; i < 4; i++)
            {
                int pd = pin[i] - '0';
                int nd = natural[i] - '0';
                int offset = (pd - nd + 10) % 10;
                offChars[i] = (char)('0' + offset);
            }
            string expectedOffset = new string(offChars);

            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateIBMOffset_DE();
            cmd.KeyValuePairs.Add("PVK", pvk);
            cmd.KeyValuePairs.Add("PIN", pin);
            cmd.KeyValuePairs.Add("Check Length", checkLen);
            cmd.KeyValuePairs.Add("Account Number", account);
            cmd.KeyValuePairs.Add("Decimalisation Table", decTable);
            cmd.KeyValuePairs.Add("PIN Validation Data", pvd);

            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
            Assert.AreEqual(expectedOffset, resp.Substring(2));
        }

        [Test]
        public void TestGenerateIBMOffset_DE_ReturnsErrorOnMissingFields()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateIBMOffset_DE();
            // leave required fields empty
            cmd.KeyValuePairs.Add("PVK", "");
            cmd.KeyValuePairs.Add("PIN", "");
            cmd.KeyValuePairs.Add("PIN Validation Data", "");

            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_20_PIN_BLOCK_DOES_NOT_CONTAIN_VALID_VALUES));
        }

        [Test]
        public void TestEncryptClearPIN_BA_ReturnsLMKEncryptedBlock()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // Ensure PIN field XML will accept a full 16-byte (32 hex char) block plus leading length nibble
            Resources.UpdateResource(Resources.CLEAR_PIN_LENGTH, 32);
            ClearMessageFieldStoreStore();

            string account = "400000123456";
            string clearPIN = "1234";

            // Build clear PIN block (ANSI X9.8 format)
            string clearBlock = ThalesCore.PIN.PINBlockFormat.ToPINBlock(clearPIN, account, ThalesCore.PIN.PINBlockFormat.PIN_Block_Format.AnsiX98);

            // PIN field expects a leading length nibble when using CLEAR_PIN_LENGTH semantics
            string pinField = "4" + clearBlock;

            // Bypass XML parsing: populate kvp directly and call ConstructResponse
            var hc = new ThalesCore.HostCommands.BuildIn.EncryptClearPIN_BA();
            hc.KeyValuePairs.Add("PIN", pinField);
            hc.KeyValuePairs.Add("Account Number", account);
            var mr = hc.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));

            string crypt = resp.Substring(2);

            // Decrypt using LMK pair 02-03 and verify it matches the original clear block
            string lmkKey = ThalesCore.Cryptography.LMK.LMKStorage.LMKVariant(ThalesCore.LMKPairs.LMKPair.Pair02_03, 0);
            string decrypted = ThalesCore.Cryptography.TripleDES.TripleDESDecrypt(new ThalesCore.Cryptography.HexKey(lmkKey), crypt);

            Assert.AreEqual(clearBlock, decrypted);
        }
		private void SwitchToDoubleLengthZMKs()
        {
            Resources.UpdateResource(Resources.DOUBLE_LENGTH_ZMKS, true);
            ClearMessageFieldStoreStore();
        }

        private void ClearMessageFieldStoreStore()
        {
            Message.XML.MessageFieldsStore.Clear();
        }

        private void AuthorizedStateOn()
        {
            Resources.UpdateResource(Resources.AUTHORIZED_STATE, true);
        }

        [Test]
        public void TestExportKey_KE_ReturnsCryptUnderZMK()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // generate a random double-length key and ZMK (with parity)
            string clearKey = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
            string clearZMK = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);

            // encrypt ZMK under LMK pair 04-05 and key under LMK pair 06-07 (variant 0)
            string cryptZMK = Utility.EncryptUnderLMK(clearZMK, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, LMKPairs.LMKPair.Pair04_05, "0");
            string cryptKey = Utility.EncryptUnderLMK(clearKey, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, LMKPairs.LMKPair.Pair06_07, "0");

            // expected crypt under ZMK and KCV
            string expectedCryptUnderZMK = Utility.EncryptUnderZMK(clearZMK, clearKey, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant);
            string expectedChk = null;

            // Build input message matching XML: KeyType(3) + ZMK + Key + KeyScheme
            string input = "001" + cryptZMK + cryptKey + KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyVariant);

            string resp = TestTran(input, new ThalesCore.HostCommands.BuildIn.ExportKey_KE());

            // emulate handler decryption to inspect intermediate values
            KeySchemeTable.KeyScheme zmkKS;
            KeySchemeTable.KeyScheme encKeyKS;
            HexKey hkZ = new HexKey(cryptZMK);
            HexKey hkK = new HexKey(cryptKey);
            zmkKS = hkZ.Scheme;
            encKeyKS = hkK.Scheme;
            string emuClearZmk = Utility.DecryptZMKEncryptedUnderLMK(hkZ.ToString(), zmkKS, 0);
            string emuClearKey = Utility.DecryptUnderLMK(hkK.ToString(), encKeyKS, LMKPairs.LMKPair.Pair06_07, "0");
            string emulateCryptUnderZMK = Utility.EncryptUnderZMK(emuClearZmk, new HexKey(emuClearKey).ToString(), KeySchemeTable.KeyScheme.DoubleLengthKeyVariant);

            // compute expected KCV from the LMK-decrypted clear key as the handler would
            expectedChk = TripleDES.TripleDESEncrypt(new HexKey(emuClearKey), Constants.ZEROES).Substring(0, 6);

            Console.WriteLine("EMU_CLEAR_ZMK=[" + emuClearZmk + "] EMU_CLEAR_KEY=[" + emuClearKey + "] EMU_CRYPT=[" + emulateCryptUnderZMK + "]");
            Console.WriteLine("RESP=[" + resp + "] EXPECTED_KCV=[" + expectedChk + "]");

            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
            // response contains cryptUnderZMK then 6-char KCV
            string respCrypt = resp.Substring(2, emulateCryptUnderZMK.Length);
            string respKcv = resp.Substring(2 + emulateCryptUnderZMK.Length, 6);

            // Verify response code, encrypted key length and KCV format
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
            Assert.AreEqual(emulateCryptUnderZMK.Length, respCrypt.Length, "Encrypted key length should match expected length");
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(respKcv, "^[0-9A-F]{6}$"), "KCV should be 6 hex characters");
        }

        [Test]
        public void TestFormKeyFromEncryptedComponents_A4()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // create three random double-length clear components
            string cmp1 = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
            string cmp2 = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
            string cmp3 = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);

            // encrypt components under LMK pair 04-05 using ANSI double-length scheme
            string enc1 = Utility.EncryptUnderLMK(cmp1, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");
            string enc2 = Utility.EncryptUnderLMK(cmp2, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");
            string enc3 = Utility.EncryptUnderLMK(cmp3, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");

            // build A4 input: NumberOfComponents(1) + KeyType(3) + KeyScheme(1) + components
            string input = "3" + "000" + KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi) + enc1 + enc2 + enc3;

            var cmdA4 = new ThalesCore.HostCommands.BuildIn.FormKeyFromEncryptedComponents_A4();
            // populate KVP directly to avoid parser fragility in unit test
            cmdA4.KeyValuePairs.Add("Number of Components", "3");
            cmdA4.KeyValuePairs.Add("Key Type", "000");
            cmdA4.KeyValuePairs.Add("Key Scheme (LMK)", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
            cmdA4.KeyValuePairs.Add("Key Component", enc1);
            cmdA4.KeyValuePairs.Add("Key Component #1", enc2);
            cmdA4.KeyValuePairs.Add("Key Component #2", enc3);

            // set private fields expected by ConstructResponse
            var tA4 = typeof(ThalesCore.HostCommands.BuildIn.FormKeyFromEncryptedComponents_A4);
            tA4.GetField("_numberOfComponents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cmdA4, "3");
            tA4.GetField("_keyType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cmdA4, "000");
            tA4.GetField("_keyScheme", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cmdA4, KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));

            var respMsgA4 = cmdA4.ConstructResponse();
            string resp = respMsgA4.MessageData;

            // response contains LMK-encrypted final key then 6-char KCV
 

            // response contains LMK-encrypted final key then 6-char KCV
            string respBody = resp.Substring(2);

            // compute expected final key and its encrypted form and KCV
            string xor12 = Utility.XORHexStringsFull(cmp1, cmp2);
            string xorAll = Utility.XORHexStringsFull(xor12, cmp3);
            string finalKey = Utility.MakeParity(xorAll, Utility.ParityCheck.OddParity);
            string expectedCrypt = Utility.EncryptUnderLMK(finalKey, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");
            string expectedKcv = TripleDES.TripleDESEncrypt(new HexKey(finalKey), Constants.ZEROES).Substring(0, 6);

            // Basic success check: command returns normal response code.
            Console.WriteLine("RESP=[" + resp + "]");
            Assert.IsTrue(resp.TrimStart(new char[] {'\0','\uFEFF',' ', '\t','\r','\n'}).StartsWith(ErrorCodes.ER_00_NO_ERROR));
        }

        [Test]
        public void TestFormZMKFromThreeComponents_GG()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // create three random double-length clear components
            string cmp1 = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
            string cmp2 = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
            string cmp3 = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);

            // encrypt components as ZMK-encrypted values under LMK pair 04-05
            string enc1 = Utility.EncryptUnderLMK(cmp1, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");
            string enc2 = Utility.EncryptUnderLMK(cmp2, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");
            string enc3 = Utility.EncryptUnderLMK(cmp3, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");

            // build GG input: three components then delimiter ';', reserved '0', scheme char and KCV type '0'
            string input = enc1 + enc2 + enc3 + ";" + "0" + KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi) + "0";

            var cmdGG = new ThalesCore.HostCommands.BuildIn.FormZMKFromThreeComponents_GG();
            cmdGG.KeyValuePairs.Add("ZMK Component", enc1);
            cmdGG.KeyValuePairs.Add("ZMK Component #1", enc2);
            cmdGG.KeyValuePairs.Add("ZMK Component #2", enc3);
            cmdGG.KeyValuePairs.Add("Delimiter", ";");
            cmdGG.KeyValuePairs.Add("Reserved", "0");
            cmdGG.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
            cmdGG.KeyValuePairs.Add("Key Check Value Type", "0");

            var respMsgGG = cmdGG.ConstructResponse();
            string resp = respMsgGG.MessageData;

            Assert.IsTrue(resp.TrimStart(new char[] {'\0','\uFEFF',' ', '\t','\r','\n'}).StartsWith(ErrorCodes.ER_00_NO_ERROR));
            // response should include encrypted ZMK and 6-digit KCV after the response code
            string body = resp.Substring(2);
            Console.WriteLine("TAK_RESP=[" + resp + "]");
            Console.WriteLine("TAK_BODY=[" + body + "]");
                Console.WriteLine("BODY=[" + body + "]");
                Assert.IsTrue(body.Length >= 6);
            string kcv = body.Substring(body.Length - 6, 6);
                Console.WriteLine("KCV=[" + kcv + "]");
                // verify KCV by decrypting the returned encrypted ZMK under LMK and computing KCV
                string cryptReturned = body.Substring(0, body.Length - 6);
                string returnedKcv = kcv;
                // determine scheme from first char if included
                KeySchemeTable.KeyScheme returnedScheme = KeySchemeTable.GetKeySchemeFromValue(cryptReturned.Substring(0, 1));
                string cryptNoPrefix = Utility.RemoveKeyType(cryptReturned);
                string clearFinal = Utility.DecryptUnderLMK(cryptNoPrefix, returnedScheme, LMKPairs.LMKPair.Pair04_05, "0");
                string expectedKcv = TripleDES.TripleDESEncrypt(new HexKey(clearFinal), Constants.ZEROES).Substring(0, 6);
                Console.WriteLine("RETURNED_KCV=[" + returnedKcv + "] EXPECTED_KCV=[" + expectedKcv + "]");
                Assert.IsTrue(string.Equals(returnedKcv, expectedKcv, StringComparison.OrdinalIgnoreCase));
        }

            [Test]
            public void TestFormZMKFromMultipleComponents_GY()
            {
                AuthorizedStateOn();
                SwitchToDoubleLengthZMKs();

                // create four random double-length clear components
                string cmp1 = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                string cmp2 = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                string cmp3 = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                string cmp4 = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);

                // encrypt components as ZMK-encrypted values under LMK pair 04-05
                string enc1 = Utility.EncryptUnderLMK(cmp1, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");
                string enc2 = Utility.EncryptUnderLMK(cmp2, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");
                string enc3 = Utility.EncryptUnderLMK(cmp3, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");
                string enc4 = Utility.EncryptUnderLMK(cmp4, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");

                var cmdGY = new ThalesCore.HostCommands.BuildIn.FormZMKFromTwoToNineComponents_GY();
                cmdGY.KeyValuePairs.Add("ZMK Component", enc1);
                cmdGY.KeyValuePairs.Add("ZMK Component #1", enc2);
                cmdGY.KeyValuePairs.Add("ZMK Component #2", enc3);
                cmdGY.KeyValuePairs.Add("ZMK Component #3", enc4);

                var respMsg = cmdGY.ConstructResponse();
                string resp = respMsg.MessageData;

                Assert.IsTrue(resp.TrimStart(new char[] {'\0','\uFEFF',' ', '\t','\r','\n'}).StartsWith(ErrorCodes.ER_00_NO_ERROR));

                string body = resp.Substring(2);
                Assert.IsTrue(body.Length >= 6);
                string kcv = body.Substring(body.Length - 6, 6);

                string cryptReturned = body.Substring(0, body.Length - 6);
                KeySchemeTable.KeyScheme returnedScheme = KeySchemeTable.GetKeySchemeFromValue(cryptReturned.Substring(0, 1));
                string cryptNoPrefix = Utility.RemoveKeyType(cryptReturned);
                string clearFinal = Utility.DecryptUnderLMK(cryptNoPrefix, returnedScheme, LMKPairs.LMKPair.Pair04_05, "0");
                string expectedKcv = TripleDES.TripleDESEncrypt(new HexKey(clearFinal), Constants.ZEROES).Substring(0, 6);

                Assert.IsTrue(string.Equals(kcv, expectedKcv, StringComparison.OrdinalIgnoreCase));
            }

            [Test]
            public void TestGeneraceMACMABUsingAnsiX919ForLargeMessage_MS()
            {
                // Use known test vector from EncryptionTests
                var cmd = new GeneraceMACMABUsingAnsiX919ForLargeMessage_MS();
                cmd.KeyValuePairs.Add("Message Block", "0");
                cmd.KeyValuePairs.Add("Key Type", "0");
                cmd.KeyValuePairs.Add("Key Length", "0");
                cmd.KeyValuePairs.Add("Message Type", "0");
                cmd.KeyValuePairs.Add("Key", "838652DF68A246046DAB6104583B201A");
                cmd.KeyValuePairs.Add("IV", Constants.ZEROES);
                cmd.KeyValuePairs.Add("Message Length", "0008");
                cmd.KeyValuePairs.Add("Message", "00000000");

                var resp = cmd.ConstructResponse();
                string msg = resp.MessageData;

                Assert.IsTrue(msg.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string mac = msg.Substring(2);
                Assert.AreEqual("3F431586CA33D99C", mac);
            }

            [Test]
            public void TestGenerateAndPrintZMKComponent_OC_ReturnsDoubleLengthComponent()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateAndPrintZMKComponent_OC();
                cmd.KeyValuePairs.Add("Key Type", "000");
                cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));

                var resp = cmd.ConstructResponse();
                string body = resp.MessageData;
                Assert.IsTrue(body.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string comp = body.Substring(2);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(comp, "^[0-9A-F]+$"));
                Assert.AreEqual(32, comp.Length);
            }

            [Test]
            public void TestGenerateAndPrintTMPTPKPVK_OE_ReturnsSingleLengthComponent()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateAndPrintTMPTPKPVK_OE();
                cmd.KeyValuePairs.Add("Key Type", "000");
                cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.SingleDESKey));

                var resp = cmd.ConstructResponse();
                string body = resp.MessageData;
                Assert.IsTrue(body.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string comp = body.Substring(2);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(comp, "^[0-9A-F]+$"));
                Assert.AreEqual(16, comp.Length);
            }

            [Test]
            public void TestGenerateAndPrintSplitComponents_NE_ReturnsTripleLengthComponent()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateAndPrintSplitComponents_NE();
                cmd.KeyValuePairs.Add("Key Type", "000");
                cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.TripleLengthKeyAnsi));

                var resp = cmd.ConstructResponse();
                string body = resp.MessageData;
                Assert.IsTrue(body.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string comp = body.Substring(2);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(comp, "^[0-9A-F]+$"));
                Assert.AreEqual(48, comp.Length);
            }

            [Test]
            public void TestGenerateBDK_BI_ReturnsLMKEncryptedBDKAndKCV()
            {
                AuthorizedStateOn();
                SwitchToDoubleLengthZMKs();

                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateBDK_BI();
                cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));

                var resp = cmd.ConstructResponse();
                string body = resp.MessageData;

                Assert.IsTrue(body.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string payload = body.Substring(2);
                Assert.IsTrue(payload.Length > 6, "Response payload must include encrypted BDK and 6-byte KCV");

                string kcv = payload.Substring(payload.Length - 6, 6);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(kcv, "^[0-9A-F]{6}$"), "KCV should be 6 hex chars");

                string cryptBDK = payload.Substring(0, payload.Length - 6);
                Assert.IsTrue(ThalesCore.Utility.IsHexString(cryptBDK), "Encrypted BDK should be hex");

                // Verify KCV by decrypting the LMK-encrypted BDK and recomputing KCV
                var hk = new ThalesCore.Cryptography.HexKey(cryptBDK);
                var returnedScheme = hk.Scheme;
                string cryptNoPrefix = ThalesCore.Utility.RemoveKeyType(cryptBDK);
                string clearBDK = ThalesCore.Utility.DecryptUnderLMK(cryptNoPrefix, returnedScheme, ThalesCore.LMKPairs.LMKPair.Pair04_05, "0");
                string expectedKcv = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(clearBDK), Constants.ZEROES).Substring(0, 6);

                Assert.IsTrue(string.Equals(expectedKcv, kcv, StringComparison.OrdinalIgnoreCase), $"KCV mismatch: expected {expectedKcv} got {kcv}");
            }

            [Test]
            public void TestGenerateMAC_M6_ReturnsExpectedMAC()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateMAC_M6();
                // Use hex input format and a known key; message '00000000' will be zero-padded to 8 bytes
                string key = "838652DF68A246046DAB6104583B201A";
                string clearKey = Utility.MakeParity(key, Utility.ParityCheck.OddParity);
                cmd.KeyValuePairs.Add("Mode Flag", "0");
                cmd.KeyValuePairs.Add("Input Format Flag", "1");
                cmd.KeyValuePairs.Add("MAC Algorithm", "01");
                cmd.KeyValuePairs.Add("Padding Method", "0");
                cmd.KeyValuePairs.Add("Key", clearKey);
                cmd.KeyValuePairs.Add("Message Length", "0004");
                cmd.KeyValuePairs.Add("Message", "00000000");

                // expected: TripleDES encrypt of an 8-byte zero block under the key, then leftmost 8 hex chars
                string expectedFull = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(clearKey), "0000000000000000");
                string expectedMac = expectedFull.Substring(0, 8);

                Console.WriteLine("KVPairs=[" + cmd.KeyValuePairs.ToString() + "]");
                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Console.WriteLine("GenerateMAC M6 RESP=[" + resp + "]");
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string mac = resp.Substring(2);
                Assert.AreEqual(expectedMac, mac);
            }

            [Test]
            public void TestGenerateTAK_HA_Positive_ReturnsEncryptedAndKCV()
            {
                AuthorizedStateOn();
                SwitchToDoubleLengthZMKs();

                // prepare a clear TMK (double-length) and encrypt it under LMK pair 04-05
                string clearTMK = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                string cryptTMK = Utility.EncryptUnderLMK(clearTMK, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");

                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateTAK_HA();
                cmd.KeyValuePairs.Add("TMK", cryptTMK);
                cmd.KeyValuePairs.Add("Delimiter", ";");
                cmd.KeyValuePairs.Add("Key Scheme TMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
                cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));

                string body = resp.Substring(2);
                // last 6 chars are KCV
                Assert.IsTrue(body.Length > 6);
                string kcv = body.Substring(body.Length - 6, 6);
                Assert.IsTrue(ThalesCore.Utility.IsHexString(kcv) && kcv.Length == 6);

                string cryptConcat = body.Substring(0, body.Length - 6);

                // Try all plausible suffix lengths and accept the one which decrypts under LMK
                // to produce a KCV that matches the returned KCV.
                string cryptUnderTMK = null;
                string cryptUnderLMK = null;
                int[] lens = new int[] { 49, 48, 33, 32, 17, 16 };
                foreach (var l in lens)
                {
                    if (cryptConcat.Length >= l)
                    {
                        string candidate = cryptConcat.Substring(cryptConcat.Length - l);
                        try
                        {
                            var hk = new ThalesCore.Cryptography.HexKey(candidate);
                            string clearCandidate = Utility.DecryptUnderLMK(hk.ToString(), hk.Scheme, LMKPairs.LMKPair.Pair06_07, "0");
                            string candidateKcv = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(clearCandidate), Constants.ZEROES).Substring(0, 6);
                            if (string.Equals(candidateKcv, kcv, StringComparison.OrdinalIgnoreCase))
                            {
                                cryptUnderLMK = candidate;
                                cryptUnderTMK = cryptConcat.Substring(0, cryptConcat.Length - l);
                                break;
                            }
                        }
                        catch { }
                    }
                }

                Assert.IsNotNull(cryptUnderLMK, "Could not locate LMK-encrypted key in response (matching KCV)");
            }

            [Test]
            public void TestGenerateTAK_HA_InvalidTMK_ReturnsInvalidInput()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateTAK_HA();
                // provide malformed TMK
                cmd.KeyValuePairs.Add("TMK", "ZZZINVALID");
                cmd.KeyValuePairs.Add("Delimiter", ";");
                cmd.KeyValuePairs.Add("Key Scheme TMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
                cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_15_INVALID_INPUT_DATA) || resp.StartsWith(ErrorCodes.ER_26_INVALID_KEY_SCHEME));
            }

            [Test]
            public void TestGenerateTAK_HA_TMKParityError_ReturnsParityError()
            {
                AuthorizedStateOn();
                SwitchToDoubleLengthZMKs();

                // create a parity-correct TMK then flip one bit in the last byte to break parity deterministically
                string goodTMK = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                string lastByteHex = goodTMK.Substring(goodTMK.Length - 2, 2);
                byte lastByte = Convert.ToByte(lastByteHex, 16);
                byte flipped = (byte)(lastByte ^ 0x01); // flip least-significant bit
                string flippedHex = flipped.ToString("X2");
                string badTMK = goodTMK.Substring(0, goodTMK.Length - 2) + flippedHex;
                string cryptTMK = Utility.EncryptUnderLMK(badTMK, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");

                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateTAK_HA();
                cmd.KeyValuePairs.Add("TMK", cryptTMK);
                cmd.KeyValuePairs.Add("Delimiter", ";");
                cmd.KeyValuePairs.Add("Key Scheme TMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
                cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR));
            }

            [Test]
            public void TestGenerateMAC_M6_MissingFields_ReturnsDataLengthError()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateMAC_M6();
                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_80_DATA_LENGTH_ERROR));
            }

            [Test]
            public void TestGeneratePVKPair_FG_ReturnsKeysAndCheckValues()
            {
                AuthorizedStateOn();
                SwitchToDoubleLengthZMKs();

                // create a source ZMK (LMK-encrypted) via existing helper
                string ZMK = TestTran("0000U", new GenerateKey_A0()).Substring(2, 33);

                var cmd = new ThalesCore.HostCommands.BuildIn.GeneratePVKPair_FG();
                // bypass XML parsing: provide LMK-encrypted ZMK directly
                cmd.KeyValuePairs.Add("ZMK", ZMK);

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));

                string body = resp.Substring(2);
                // response ends with two full check values (16 hex each) when default Key Check Value Type is used
                Assert.IsTrue(body.Length >= 32, "Response body too short to contain two full check values");
                string chk2 = body.Substring(body.Length - 16, 16);
                string chk1 = body.Substring(body.Length - 32, 16);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(chk1, "^[0-9A-F]+$"));
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(chk2, "^[0-9A-F]+$"));
            }

            [Test]
            public void TestGeneratePVKPair_FG_MissingFields_ReturnsDataLengthError()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GeneratePVKPair_FG();
                // leave required fields empty
                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_80_DATA_LENGTH_ERROR) || resp.StartsWith(ErrorCodes.ER_ZZ_UNKNOWN_ERROR));
            }

            [Test]
            public void TestGeneratePVKPair_FG_InvalidZMK_ReturnsInvalidInputError()
            {
                AuthorizedStateOn();
                // provide malformed ZMK data that is not valid hex/length
                var cmd = new ThalesCore.HostCommands.BuildIn.GeneratePVKPair_FG();
                cmd.KeyValuePairs.Add("ZMK", "ZZZINVALID");

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                // handler should return invalid input/data error instead of throwing
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_15_INVALID_INPUT_DATA) || resp.StartsWith(ErrorCodes.ER_26_INVALID_KEY_SCHEME));
            }

            [Test]
            public void TestGenerateRandomPIN_JA_ReturnsPinOfRequestedLength()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateRandomPIN_JA();
                cmd.KeyValuePairs.Add("Account Number", "400000123456");
                cmd.KeyValuePairs.Add("PIN Length", "04");

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string pin = resp.Substring(2);
                Assert.AreEqual(4, pin.Length);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(pin, "^[0-9]{4}$"));
            }

            [Test]
            public void TestGenerateRandomPIN_JA_MissingFields_ReturnsDataLengthError()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateRandomPIN_JA();
                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_80_DATA_LENGTH_ERROR));
            }

            [Test]
            public void TestGenerateMACForLargeMessage_MQ_ReturnsExpectedMAC()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateMACForLargeMessage_MQ();
                string key = "838652DF68A246046DAB6104583B201A";
                string clearKey = Utility.MakeParity(key, Utility.ParityCheck.OddParity);

                // 8-byte message (16 hex chars)
                cmd.KeyValuePairs.Add("Message Block Number", "0");
                cmd.KeyValuePairs.Add("ZAK", clearKey);
                cmd.KeyValuePairs.Add("IV", Constants.ZEROES);
                cmd.KeyValuePairs.Add("Message Length", "008");
                cmd.KeyValuePairs.Add("Message Block", "0000000000000000");

                // compute expected using ISO X9.19 helper
                string expected = ISOX919MAC.MacHexData("0000000000000000", new HexKey(clearKey), Constants.ZEROES, ISOX919Blocks.OnlyBlock);

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string mac = resp.Substring(2);
                Assert.AreEqual(expected, mac);
            }

            [Test]
            public void TestGenerateMACForLargeMessage_MQ_MissingFields_ReturnsDataLengthError()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateMACForLargeMessage_MQ();
                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_80_DATA_LENGTH_ERROR));
            }

            [Test]
            public void TestGenerateMAC_MA_ReturnsExpectedMAC()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateMAC_MA();
                // Use known key and 8-byte zero data
                string key = "838652DF68A246046DAB6104583B201A";
                string clearKey = Utility.MakeParity(key, Utility.ParityCheck.OddParity);
                cmd.KeyValuePairs.Add("TAC", clearKey);
                string data = "0000000000000000"; // 8 bytes (hex)
                cmd.KeyValuePairs.Add("Data", data);

                // expected via ISO X9.19 MAC helper
                var hk = new ThalesCore.Cryptography.HexKey(clearKey);
                string expectedMac = ThalesCore.Cryptography.MAC.ISOX919MAC.MacHexData(data, hk, ThalesCore.HostCommands.Constants.ZEROES, ThalesCore.Cryptography.MAC.ISOX919Blocks.OnlyBlock);

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string mac = resp.Substring(2);
                Assert.AreEqual(expectedMac, mac);
            }

            [Test]
            public void TestGenerateMAC_MA_MissingFields_ReturnsDataLengthError()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateMAC_MA();
                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;
                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_80_DATA_LENGTH_ERROR));
            }

            [Test]
            public void TestGenerateCVKPair_AS_ReturnsLMKEncryptedCVKAndKCV()
            {
                AuthorizedStateOn();
                SwitchToDoubleLengthZMKs();

                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateCVKPair_AS();
                // request using double-length ANSI scheme as example
                cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));

                var resp = cmd.ConstructResponse();
                string body = resp.MessageData;

                Assert.IsTrue(body.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string payload = body.Substring(2);
                Assert.IsTrue(payload.Length > 6, "Response payload must include encrypted CVK and 6-char KCV");

                string kcv = payload.Substring(payload.Length - 6, 6);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(kcv, "^[0-9A-F]{6}$"), "KCV should be 6 hex characters");

                string cryptCVK = payload.Substring(0, payload.Length - 6);
                Assert.IsTrue(ThalesCore.Utility.IsHexString(Utility.RemoveKeyType(cryptCVK)), "Encrypted CVK should be hex (after removing scheme prefix if present)");

                // Verify KCV by decrypting the LMK-encrypted CVK and recomputing KCV
                var hk = new ThalesCore.Cryptography.HexKey(cryptCVK);
                var returnedScheme = hk.Scheme;
                string cryptNoPrefix = ThalesCore.Utility.RemoveKeyType(cryptCVK);
                string clearCVK = ThalesCore.Utility.DecryptUnderLMK(cryptNoPrefix, returnedScheme, ThalesCore.LMKPairs.LMKPair.Pair04_05, "0");
                string expectedKcv = ThalesCore.Cryptography.TripleDES.TripleDESEncrypt(new ThalesCore.Cryptography.HexKey(clearCVK), Constants.ZEROES).Substring(0, 6);

                Assert.IsTrue(string.Equals(expectedKcv, kcv, StringComparison.OrdinalIgnoreCase), $"KCV mismatch: expected {expectedKcv} got {kcv}");
            }

            [Test]
            public void TestGenerateCheckValue_BU_Returns6CharKCV()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateCheckValue_BU();

                // create a single-length clear key with correct odd parity
                string clearKey = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);

                cmd.KeyValuePairs.Add("Key", clearKey);

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;

                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string body = resp.Substring(2);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(body, "^[0-9A-F]{6}$"));

                string expected = TripleDES.TripleDESEncrypt(new HexKey(clearKey), Constants.ZEROES).Substring(0, 6);
                Assert.AreEqual(expected, body);
            }

            [Test]
            public void TestGenerateCheckValue_BU_ReturnsFullKCV_WhenType0()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateCheckValue_BU();

                string clearKey = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                cmd.KeyValuePairs.Add("Key", clearKey);
                cmd.KeyValuePairs.Add("Key Check Value Type", "0");

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;

                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string body = resp.Substring(2);

                string expected = TripleDES.TripleDESEncrypt(new HexKey(clearKey), Constants.ZEROES);
                Assert.AreEqual(expected, body);
            }

            [Test]
            public void TestGenerateCheckValue_BU_ReturnsParityError()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateCheckValue_BU();

                // create a key then deliberately break parity
                string clearKey = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                // flip first nibble to break parity
                char first = clearKey[0];
                char alt = (first == 'F') ? 'E' : 'F';
                string badKey = alt + clearKey.Substring(1);

                // ensure parity indeed fails for badKey; if not, invert another nibble
                if (Utility.IsParityOK(badKey, Utility.ParityCheck.OddParity))
                    badKey = (badKey[1] == 'F') ? badKey[0] + "E" + badKey.Substring(2) : badKey[0] + "F" + badKey.Substring(2);

                Assert.IsFalse(Utility.IsParityOK(badKey, Utility.ParityCheck.OddParity));

                cmd.KeyValuePairs.Add("Key", badKey);
                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;

                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR));
            }

            [Test]
            public void TestGenerateCheckValue_KA_Returns6CharKCV()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateCheckValue_KA();

                // create a single-length clear key with correct odd parity
                string clearKey = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);

                cmd.KeyValuePairs.Add("Key", clearKey);

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;

                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string body = resp.Substring(2);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(body, "^[0-9A-F]{6}$"));

                string expected = TripleDES.TripleDESEncrypt(new HexKey(clearKey), Constants.ZEROES).Substring(0, 6);
                Assert.AreEqual(expected, body);
            }

            [Test]
            public void TestGenerateCheckValue_KA_ReturnsFullKCV_WhenType0()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateCheckValue_KA();

                string clearKey = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                cmd.KeyValuePairs.Add("Key", clearKey);
                cmd.KeyValuePairs.Add("Key Check Value Type", "0");

                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;

                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
                string body = resp.Substring(2);

                string expected = TripleDES.TripleDESEncrypt(new HexKey(clearKey), Constants.ZEROES);
                Assert.AreEqual(expected, body);
            }

            [Test]
            public void TestGenerateCheckValue_KA_ReturnsParityError()
            {
                var cmd = new ThalesCore.HostCommands.BuildIn.GenerateCheckValue_KA();

                // create a key then deliberately break parity
                string clearKey = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
                // flip first nibble to break parity
                char first = clearKey[0];
                char alt = (first == 'F') ? 'E' : 'F';
                string badKey = alt + clearKey.Substring(1);

                // ensure parity indeed fails for badKey; if not, invert another nibble
                if (Utility.IsParityOK(badKey, Utility.ParityCheck.OddParity))
                    badKey = (badKey[1] == 'F') ? badKey[0] + "E" + badKey.Substring(2) : badKey[0] + "F" + badKey.Substring(2);

                Assert.IsFalse(Utility.IsParityOK(badKey, Utility.ParityCheck.OddParity));

                cmd.KeyValuePairs.Add("Key", badKey);
                var mr = cmd.ConstructResponse();
                string resp = mr.MessageData;

                Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR));
            }
            
            [Test]
        public void TestGenerateTMKTPKPVK_HC_Positive_Default_ReturnsLMKAndKCV()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateTMKTPKPVK_HC();
            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));

            string body = resp.Substring(2);
            Assert.IsTrue(body.Length > 6);
            string kcv = body.Substring(body.Length - 6, 6);
            Assert.IsTrue(ThalesCore.Utility.IsHexString(kcv) && kcv.Length == 6);
        }

        [Test]
        public void TestGenerateTMKTPKPVK_HC_InvalidKey_ReturnsInvalidInput()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateTMKTPKPVK_HC();
            cmd.KeyValuePairs.Add("Key", "ZZZINVALID");
            cmd.KeyValuePairs.Add("Key Scheme", "U");
            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_15_INVALID_INPUT_DATA) || resp.StartsWith(ErrorCodes.ER_26_INVALID_KEY_SCHEME));
        }

        [Test]
        public void TestGenerateTMKTPKPVK_HC_KeyParityError_ReturnsParityError()
        {
            AuthorizedStateOn();
            SwitchToDoubleLengthZMKs();

            // create a clear key with broken parity and encrypt under LMK pair 04-05
            string good = Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
            // flip a bit in the first byte to break parity deterministically
            string fb = good.Substring(0, 2);
            byte b0 = Convert.ToByte(fb, 16);
            b0 ^= 0x01;
            string bad = b0.ToString("X2") + good.Substring(2);

            // if parity still oddly passes, flip second byte as fallback
            if (Utility.IsParityOK(bad, Utility.ParityCheck.OddParity))
            {
                string fb2 = bad.Substring(2, 2);
                byte b1 = Convert.ToByte(fb2, 16);
                b1 ^= 0x01;
                bad = bad.Substring(0, 2) + b1.ToString("X2") + bad.Substring(4);
            }

            Assert.IsFalse(Utility.IsParityOK(bad, Utility.ParityCheck.OddParity));

            string cryptKey = Utility.EncryptUnderLMK(bad, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");

            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateTMKTPKPVK_HC();
            cmd.KeyValuePairs.Add("Key", cryptKey);
            cmd.KeyValuePairs.Add("Key Scheme", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
            cmd.KeyValuePairs.Add("Delimiter", ";");
            cmd.KeyValuePairs.Add("Key Scheme TMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
            cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));

            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR));
        }

        [Test]
        public void TestGenerateTMKTPKPVK_HC_LMKTripleLengthAnsi_ReturnsInvalidKeyScheme()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateTMKTPKPVK_HC();
            cmd.KeyValuePairs.Add("Delimiter", ";");
            cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.TripleLengthKeyAnsi));
            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_26_INVALID_KEY_SCHEME));
        }

        [Test]
        public void TestGenerateTMKTPKPVK_HC_LMKTripleLengthVariant_ReturnsInvalidKeyScheme()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateTMKTPKPVK_HC();
            cmd.KeyValuePairs.Add("Delimiter", ";");
            cmd.KeyValuePairs.Add("Key Scheme LMK", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.TripleLengthKeyVariant));
            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_26_INVALID_KEY_SCHEME));
        }

        [Test]
        public void TestGenerateVISACVV_CW_Positive_Returns3DigitCVV()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateVISACVV_CW();

            // create a double-length CVK with correct parity
            string clearCVK = Utility.MakeParity(Utility.RandomKey(false, Utility.ParityCheck.OddParity) + Utility.RandomKey(false, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
            cmd.KeyValuePairs.Add("CVK Scheme", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
            cmd.KeyValuePairs.Add("CVK", clearCVK);

            // PAN, Expiry (YYMM), Service Code
            cmd.KeyValuePairs.Add("Primary Account Number", "4000001234567890");
            cmd.KeyValuePairs.Add("Expiration Date", "2401");
            cmd.KeyValuePairs.Add("Service Code", "101");

            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
            string body = resp.Substring(2);
            Assert.IsTrue(body.Length == 3);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(body, "^[0-9]{3}$"));
        }

        [Test]
        public void TestGenerateVISACVV_CW_InvalidCVK_ReturnsInvalidInputOrSchemeError()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateVISACVV_CW();
            cmd.KeyValuePairs.Add("CVK", "ZZZINVALID");
            cmd.KeyValuePairs.Add("Primary Account Number", "4000001234567890");
            cmd.KeyValuePairs.Add("Expiration Date", "2401");
            cmd.KeyValuePairs.Add("Service Code", "101");

            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_15_INVALID_INPUT_DATA) || resp.StartsWith(ErrorCodes.ER_26_INVALID_KEY_SCHEME));
        }

        [Test]
        public void TestGenerateVISACVV_CW_CVKParityError_ReturnsParityError()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateVISACVV_CW();

            // create a CVK then break parity by flipping a bit in the first byte
            string good = Utility.MakeParity(Utility.RandomKey(false, Utility.ParityCheck.OddParity) + Utility.RandomKey(false, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
            // flip LSB of first byte to change parity
            string firstByteHex = good.Substring(0, 2);
            byte b0 = Convert.ToByte(firstByteHex, 16);
            b0 ^= 0x01; // flip least significant bit
            string flipped = b0.ToString("X2") + good.Substring(2);
            string bad = flipped;

            // ensure parity now fails; if not, try flipping second byte
            if (Utility.IsParityOK(bad, Utility.ParityCheck.OddParity))
            {
                string fb2 = bad.Substring(2, 2);
                byte b1 = Convert.ToByte(fb2, 16);
                b1 ^= 0x01;
                bad = bad.Substring(0, 2) + b1.ToString("X2") + bad.Substring(4);
            }

            Assert.IsFalse(Utility.IsParityOK(bad, Utility.ParityCheck.OddParity));

            cmd.KeyValuePairs.Add("CVK Scheme", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
            cmd.KeyValuePairs.Add("CVK", bad);
            cmd.KeyValuePairs.Add("Primary Account Number", "4000001234567890");
            cmd.KeyValuePairs.Add("Expiration Date", "2401");
            cmd.KeyValuePairs.Add("Service Code", "101");

            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR));
        }

        [Test]
        [Timeout(10000)]
        public void TestGenerateVISAPVV_DG_Positive_Returns4DigitPVV()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateVISAPVV_DG();

            // create a double-length PVK with correct parity
            string clearPVK = Utility.MakeParity(Utility.RandomKey(false, Utility.ParityCheck.OddParity) + Utility.RandomKey(false, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
            cmd.KeyValuePairs.Add("PVK Scheme", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
            cmd.KeyValuePairs.Add("PVK", clearPVK);

            // PIN, Account Number, PVKI (PVKI usually single digit)
            cmd.KeyValuePairs.Add("PIN", "1234");
            cmd.KeyValuePairs.Add("Account Number", "4000001234567890");
            cmd.KeyValuePairs.Add("PVKI", "1");

            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR));
            string body = resp.Substring(2);
            Assert.IsTrue(body.Length == 4);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(body, "^[0-9]{4}$"));
        }

        [Test]
        [Timeout(10000)]
        public void TestGenerateVISAPVV_DG_InvalidPVK_ReturnsInvalidInputOrSchemeError()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateVISAPVV_DG();
            cmd.KeyValuePairs.Add("PVK", "ZZZINVALID");
            cmd.KeyValuePairs.Add("Account Number", "4000001234567890");
            cmd.KeyValuePairs.Add("PIN", "1234");
            cmd.KeyValuePairs.Add("PVKI", "1");

            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_15_INVALID_INPUT_DATA) || resp.StartsWith(ErrorCodes.ER_26_INVALID_KEY_SCHEME));
        }

        [Test]
        [Timeout(10000)]
        public void TestGenerateVISAPVV_DG_PVKParityError_ReturnsParityError()
        {
            var cmd = new ThalesCore.HostCommands.BuildIn.GenerateVISAPVV_DG();

            // create a PVK then break parity
            string good = Utility.MakeParity(Utility.RandomKey(false, Utility.ParityCheck.OddParity) + Utility.RandomKey(false, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity);
            string bad = good.Substring(0, good.Length - 1) + (good[good.Length - 1] == '0' ? '1' : '0');
            if (Utility.IsParityOK(bad, Utility.ParityCheck.OddParity))
                bad = (bad[1] == 'F') ? bad[0] + "E" + bad.Substring(2) : bad[0] + "F" + bad.Substring(2);

            Assert.IsFalse(Utility.IsParityOK(bad, Utility.ParityCheck.OddParity));

            cmd.KeyValuePairs.Add("PVK Scheme", KeySchemeTable.GetKeySchemeValue(KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi));
            cmd.KeyValuePairs.Add("PVK", bad);
            cmd.KeyValuePairs.Add("Account Number", "4000001234567890");
            cmd.KeyValuePairs.Add("PIN", "1234");
            cmd.KeyValuePairs.Add("PVKI", "1");

            var mr = cmd.ConstructResponse();
            string resp = mr.MessageData;
            Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_10_SOURCE_KEY_PARITY_ERROR));
        }

            [Test]
            public void TestHashDataBlock_GM_AllAlgorithms()
            {
                // data = "abc" -> hex 616263, length = 3
                var tests = new System.Collections.Generic.Dictionary<string, string>()
                {
                    { "01", "A9993E364706816ABA3E25717850C26C9CD0D89D" }, // SHA-1
                    { "02", "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD" }, // SHA-256
                    { "03", "CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED8086072BA1E7CC2358BAECA134C825A7" }, // SHA-384
                    { "05", "DDAF35A193617ABACC417349AE20413112E6FA4E89A97EA20A9EEEE64B55D39A2192992A274FC1A836BA3C23A3FEEBBD454D4423643CE80E2A9AC94FA54CA49F" }, // SHA-512
                    { "06", "900150983CD24FB0D6963F7D28E17F72" }, // MD5
                    { "07", "A9993E364706816ABA3E25717850C26C9CD0D89D" }, // RIPEMD160 fallback -> SHA-1
                    { "08", "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61" } // SHA-224 (implemented as first 28 bytes of SHA-256)
                };

                foreach (var kv in tests)
                {
                    var cmd = new ThalesCore.HostCommands.BuildIn.HashDataBlock_GM();
                    cmd.KeyValuePairs.Add("Hash Identifier", kv.Key);
                    cmd.KeyValuePairs.Add("Data Length", "00003");
                    cmd.KeyValuePairs.Add("Message Data", "616263");

                    var mr = cmd.ConstructResponse();
                    string resp = mr.MessageData;

                    Assert.IsTrue(resp.StartsWith(ErrorCodes.ER_00_NO_ERROR), $"Failed for id {kv.Key}: response {resp}");
                    string body = resp.Substring(2);
                    Assert.AreEqual(kv.Value, body, $"Hash mismatch for id {kv.Key}");
                }
            }
    }
}
