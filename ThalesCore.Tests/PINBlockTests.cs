using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ThalesCore.PIN;

namespace ThalesCore.Tests
{
    [TestClass]
    public class PINBlockTests
    {
        private TestContext testContextInstance;

        [TestMethod]
        public void TestANSIX98Creation()
        {
            string s = PINBlockFormat.ToPINBlock("4324", "00000000819420823", PINBlockFormat.PIN_Block_Format.AnsiX98);
            Assert.AreEqual(PINBlockFormat.ToPINBlock("1234", "550000025321", PINBlockFormat.PIN_Block_Format.AnsiX98), "041261FFFFFDACDE");
        }

        [TestMethod]
        public void TestDieboldCreation()
        {
            Assert.AreEqual(PINBlockFormat.ToPINBlock("1234", "550000025321", PINBlockFormat.PIN_Block_Format.Diebold), "1234FFFFFFFFFFFF");
        }

        [TestMethod]
        public void TestANSIX98Decomposition()
        {
            Assert.AreEqual(PINBlockFormat.ToPIN("041261FFFFFDACDE", "550000025321", PINBlockFormat.PIN_Block_Format.AnsiX98), "1234");
        }

        [TestMethod]
        public void TestDieboldDecomposition()
        {
            Assert.AreEqual(PINBlockFormat.ToPIN("1234FFFFFFFFFFFF", "550000025321", PINBlockFormat.PIN_Block_Format.Diebold), "1234");
        }


    }
}
