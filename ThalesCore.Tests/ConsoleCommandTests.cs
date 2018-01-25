using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThalesCore;
using ThalesCore.ConsoleCommands;
using ThalesCore.ConsoleCommands.Implementations;
using ThalesCore.Cryptography;
using ThalesCore.ConsoleCommands.Validators;

namespace ThalesCore.Tests
{
    [TestClass]
    public class ConsoleCommandTests
    {
        private ThalesMain o;

        private const string ZEROES = "0000000000000000";


        [TestInitialize]
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

        [TestCleanup]
        public void EndTests()
        {
            o.ShutDown();
            o = null;
        }

        private string TestCommand(string[] input, AConsoleCommand CC)
        {
            CC.InitializeStack();

            string ret = "";

            if (CC.IsNoinputCommand())
            {
                ret = CC.ProcessMessage();
                CC = null;
            }
            else
            {
                int i = 0;
                while ((i < input.GetLength(0)) || (!CC.CommandFinished))
                {
                    CC.GetClientMessage();
                    ret = CC.AcceptMessage(input[i]);
                    i += 1;
                }
            }


            return ret;
        }


        [TestMethod]
        public void TestEnterAuthorizedState()
        {
            TestCommand(new string[] { }, new EnterAuthorizedState_A());
            Assert.IsTrue(IsInAuthorizedState());

        }

        [TestMethod]
        public void TestExitAuthorizedState()
        {
            TestCommand(new string[] { }, new CancelAuthorizedState_C());
            Assert.IsTrue(IsInAuthorizedState());

        }

        [TestMethod]
        public void TestDoubleLengthDesCalculator()
        {
            HexKey k = GetRandomKey(HexKey.KeyLength.DoubleLength);
            Assert.AreEqual("Encrypted: " + Breakup(TripleDES.TripleDESEncrypt(k, ZEROES)) + System.Environment.NewLine + "Decrypted: " + Breakup(TripleDES.TripleDESDecrypt(k, ZEROES)),
                        TestCommand(new string[] { k.ToString(), ZEROES }, new DoubleLengthDESCalculator_Dollar()));
        }

        [TestMethod]
        public void TestSingleLengthDesCalculator()
        {
            HexKey k = GetRandomKey(HexKey.KeyLength.SingleLength);
            Assert.AreEqual("Encrypted: " + Breakup(TripleDES.TripleDESEncrypt(k, ZEROES)) + System.Environment.NewLine + "Decrypted: " + Breakup(TripleDES.TripleDESDecrypt(k, ZEROES)), 
                        TestCommand(new string[] { k.ToString(), ZEROES }, new SingleLengthDESCalculator_N()));
        }

        [TestMethod]
        public void TestTripleLengthDesCalculator()
        {
            HexKey k = GetRandomKey(HexKey.KeyLength.TripleLength);
            Assert.AreEqual("Encrypted: " + Breakup(TripleDES.TripleDESEncrypt(k, ZEROES)) + System.Environment.NewLine + "Decrypted: " + Breakup(TripleDES.TripleDESDecrypt(k, ZEROES)),
                        TestCommand(new string[] { k.ToString(), "S",ZEROES }, new TripleLengthDESCalculator_T()));
        }

        [TestMethod]
        public void TestEncryptClearComponent()
        {
            AuthorizedStateOn();
            HexKey k = GetRandomKey(HexKey.KeyLength.DoubleLength);
            Assert.AreEqual("Encrypted Component: " + Breakup(Utility.EncryptUnderLMK(k.ToString(), KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, LMKPairs.LMKPair.Pair04_05, "0")) + System.Environment.NewLine + 
                        "Key check value: " + Breakup(TripleDES.TripleDESEncrypt(k, ZEROES).Substring(0, 6)), 
                        TestCommand(new string[] { "000", "U", k.ToString()}, new EncryptClearComponent_EC()));
            Assert.AreEqual("Encrypted Component: " + Breakup(Utility.EncryptUnderLMK(k.ToString(), KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair06_07, "0")) + System.Environment.NewLine +
                        "Key check value: " + Breakup(TripleDES.TripleDESEncrypt(k, ZEROES).Substring(0, 6)),
                        TestCommand(new string[] { "001", "X", k.ToString() }, new EncryptClearComponent_EC()));
            k = GetRandomKey(HexKey.KeyLength.TripleLength);
            Assert.AreEqual("Encrypted Component: " + Breakup(Utility.EncryptUnderLMK(k.ToString(), KeySchemeTable.KeyScheme.TripleLengthKeyAnsi, LMKPairs.LMKPair.Pair26_27, "0")) + System.Environment.NewLine +
                            "Key check value: " + Breakup(TripleDES.TripleDESEncrypt(k, ZEROES).Substring(0, 6)),
                            TestCommand(new string[] { "008", "Y", k.ToString() }, new EncryptClearComponent_EC()));
        }

        [TestMethod]
        public void TestExportKey()
        {
            HexKey k = null;
            HexKey ZMK = null;
            string cryptZMK = "";
            string cryptKey = "";
            string cryptUnderZMK = "";

            GenerateTestKeyAndZMKKey(out k, LMKPairs.LMKPair.Pair06_07, KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, out ZMK, out cryptZMK, out cryptKey, out cryptUnderZMK);

            Assert.AreEqual("Key encrypted under ZMK: " + Breakup(cryptUnderZMK) + System.Environment.NewLine +
                        "Key Check Value: " + Breakup(TripleDES.TripleDESEncrypt(k, ZEROES).Substring(0, 6)),
                        TestCommand(new string[] { "001", "U", cryptZMK, cryptKey }, new ExportKey_KE()));
        }

        [TestMethod]
        public void TestFormKeyFromComponents()
        {
            AuthorizedStateOn();
            HexKey cmp1 = GetRandomKey(HexKey.KeyLength.DoubleLength);
            HexKey cmp2 = GetRandomKey(HexKey.KeyLength.DoubleLength);
            HexKey cmp3 = GetRandomKey(HexKey.KeyLength.DoubleLength);
            HexKey zmk = new HexKey(Utility.XORHexStringsFull(Utility.XORHexStringsFull(cmp1.ToString(), cmp2.ToString()), cmp3.ToString()));
            string cryptZMK = Utility.EncryptUnderLMK(zmk.ToString(), KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, LMKPairs.LMKPair.Pair04_05, "0");
            Assert.AreEqual("Encrypted key: " + Breakup(cryptZMK) + System.Environment.NewLine +
                            "Key check value: " + Breakup(TripleDES.TripleDESEncrypt(zmk, ZEROES).Substring(0, 6)),
                            TestCommand(new string[] { "2", "000", "U", "X", "3", cmp1.ToString(), cmp2.ToString(), cmp3.ToString() }, new FormKeyFromComponents_FK()));
        }

        [TestMethod]
        public void TestZMKFromEncryptedComponents()
        {
            AuthorizedStateOn();
            HexKey cmp1 = GetRandomKey(HexKey.KeyLength.DoubleLength);
            HexKey cmp2 = GetRandomKey(HexKey.KeyLength.DoubleLength);
            HexKey cmp3 = GetRandomKey(HexKey.KeyLength.DoubleLength);
            HexKey zmk = new HexKey(Utility.XORHexStringsFull(Utility.XORHexStringsFull(cmp1.ToString(), cmp2.ToString()), cmp3.ToString()));
            string cryptZMK = Utility.EncryptUnderLMK(zmk.ToString(), KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0");
            Assert.AreEqual("Encrypted key: " + Breakup(cryptZMK) + System.Environment.NewLine +
                        "Key check value: " + Breakup(TripleDES.TripleDESEncrypt(zmk, ZEROES).Substring(0, 6)),
                        TestCommand(new string[]  {
                "3", Utility.RemoveKeyType(Utility.EncryptUnderLMK(cmp1.ToString(), KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0")),
                                                       Utility.RemoveKeyType(Utility.EncryptUnderLMK(cmp2.ToString(), KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0")),
                                                       Utility.RemoveKeyType(Utility.EncryptUnderLMK(cmp3.ToString(), KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, LMKPairs.LMKPair.Pair04_05, "0"))}, new FormZMKFromEncryptedComponents_D()));
        }

        [TestMethod]
        public void TestImportKey()
        {
            AuthorizedStateOn();
            HexKey k;
            HexKey ZMK;
            string cryptZMK = "";
            string cryptKey = "";
            string cryptUnderZMK = "";
            GenerateTestKeyAndZMKKey(out k, LMKPairs.LMKPair.Pair06_07, KeySchemeTable.KeyScheme.DoubleLengthKeyAnsi, out ZMK, out cryptZMK, out cryptKey, out cryptUnderZMK);

            Assert.AreEqual("Key under LMK: " + Breakup(cryptKey) + System.Environment.NewLine +
                        "Key Check Value: " + Breakup(TripleDES.TripleDESEncrypt(k, ZEROES).Substring(0, 6)),
                        TestCommand(new string[] { "001", "X", cryptZMK, cryptUnderZMK }, new ImportKey_IK()));

        }

        private void GenerateTestKeyAndZMKKey(out HexKey k, LMKPairs.LMKPair kLMK, KeySchemeTable.KeyScheme kScheme, out HexKey ZMK, out string cryptZMK, out string cryptKey, out string cryptUnderZMK)
        {
            k = GetRandomKey(HexKey.KeyLength.DoubleLength);
            ZMK = GetRandomKey(HexKey.KeyLength.DoubleLength);
            cryptZMK = Utility.EncryptUnderLMK(ZMK.ToString(), KeySchemeTable.KeyScheme.DoubleLengthKeyVariant, LMKPairs.LMKPair.Pair04_05, "0");
            cryptKey = Utility.EncryptUnderLMK(k.ToString(), kScheme, kLMK, "0");
            cryptUnderZMK = Utility.EncryptUnderZMK(ZMK.ToString(), k.ToString(), kScheme);
        }

        private void AuthorizedStateOn()
        {
            Resources.UpdateResource(Resources.AUTHORIZED_STATE, true);
        }

        private string Breakup(string s)
        {
            string ret = "";
            string key = Utility.RemoveKeyType(s);
            for (int i = 0; i < key.Length; i += 4)
            {
                if (i + 4 <= key.Length)
                    ret += key.Substring(i, 4) + " ";
                else
                    ret += key.Substring(i);
            }

            if (key != s)
                return s.Substring(0, 1) + " " + ret;
            else
                return ret;
        }

        

        private HexKey GetRandomKey(HexKey.KeyLength l)
        {
            switch (l)
            {
                case HexKey.KeyLength.SingleLength:
                    return new HexKey(Utility.RandomKey(true, Utility.ParityCheck.OddParity));
                case HexKey.KeyLength.DoubleLength:
                    return new HexKey(Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity));
                default:
                    return new HexKey(Utility.MakeParity(Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity) + Utility.RandomKey(true, Utility.ParityCheck.OddParity), Utility.ParityCheck.OddParity));
            }
        }

        private bool IsInAuthorizedState()
        {
            return Convert.ToBoolean(Resources.GetResource(Resources.AUTHORIZED_STATE));
        }
    }
}
