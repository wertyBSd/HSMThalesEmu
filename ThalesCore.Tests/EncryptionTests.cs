using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThalesCore.Cryptography;
using ThalesCore.Cryptography.MAC;

namespace ThalesCore.Tests
{
    [TestClass]
    public class EncryptionTests
    {
        private const string ZEROES = "0000000000000000";



        [TestMethod]
        public void TestSimpleDES()
        {
            string sResult = DES.DESEncrypt("0123456789ABCDEF", ZEROES);
            Assert.AreEqual(sResult, "D5D44FF720683D0D");
            string sResult2 = DES.DESDecrypt("0123456789ABCDEF", sResult);
            Assert.AreEqual(sResult2, ZEROES);

        }

        [TestMethod]
        public void TestTripleDES()
        {
            string sResult = TripleDES.TripleDESEncrypt(new HexKey("0123456789ABCDEFABCDEF0123456789"), ZEROES);
            Assert.AreEqual(sResult, "EE21F1F01A3D7C9A");
            string sResult2 = TripleDES.TripleDESDecrypt(new HexKey("0123456789ABCDEFABCDEF0123456789"), sResult);
            Assert.AreEqual(sResult2, ZEROES);
        }

        [TestMethod]
        public void TestDoubleVariant()
        {
            Cryptography.LMK.LMKStorage.LMKStorageFile = "LMKSTORAGE.TXT";
            Cryptography.LMK.LMKStorage.GenerateTestLMKs();
            string sResult = TripleDES.TripleDESEncryptVariant(new HexKey(Cryptography.LMK.LMKStorage.LMKVariant(LMKPairs.LMKPair.Pair28_29, 2)), "F1F1F1F1F1F1F1F1C1C1C1C1C1C1C1C1");
            Assert.AreEqual(sResult, "5178C9D3D1052B15BF6AEC458B4A4564");
        }

        [TestMethod]
        public void TestX919Mac()
        {
            HexKey hk = new HexKey("838652DF68A246046DAB6104583B201A");
            string strdata = "30303030303030303131313131313131";
            string IV = "0000000000000000";

            string hexString = String.Empty;
            Utility.ByteArrayToHexString(Utility.GetBytesFromString("00000000"),out hexString);
            Assert.AreEqual("3F431586CA33D99C", ISOX919MAC.MacHexData(hexString, hk, IV, ISOX919Blocks.OnlyBlock));

            IV = ISOX919MAC.MacHexData(strdata, hk, IV, ISOX919Blocks.FirstBlock);
            Assert.AreEqual("A9D4D96683B51333", IV);
            IV = ISOX919MAC.MacHexData(strdata, hk, IV, ISOX919Blocks.NextBlock);
            Assert.AreEqual("DA46CEC9E61AF065", IV);
            IV = ISOX919MAC.MacHexData(strdata, hk, IV, ISOX919Blocks.NextBlock);
            Assert.AreEqual("56A27E35442BD07D", IV);
            IV = ISOX919MAC.MacHexData(strdata, hk, IV, ISOX919Blocks.NextBlock);
            Assert.AreEqual("B12874BED7137303", IV);
            IV = ISOX919MAC.MacHexData(strdata, hk, IV, ISOX919Blocks.FinalBlock);
            Assert.AreEqual("0D99127F7734AA58", IV);
        }
    }
}
