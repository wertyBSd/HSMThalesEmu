using System;
using NUnit.Framework;

using ThalesCore.PIN;

namespace ThalesCore.Tests
{
    [TestFixture]
    public class PINBlockTests
    {

        [Test]
        public void TestANSIX98Creation()
        {
            Assert.AreEqual(PINBlockFormat.ToPINBlock("1234", "550000025321", PINBlockFormat.PIN_Block_Format.AnsiX98), "041261FFFFFDACDE");
        }

        [Test]
        public void TestDieboldCreation()
        {
            Assert.AreEqual(PINBlockFormat.ToPINBlock("1234", "550000025321", PINBlockFormat.PIN_Block_Format.Diebold), "1234FFFFFFFFFFFF");
        }

        [Test]
        public void TestANSIX98Decomposition()
        {
            Assert.AreEqual(PINBlockFormat.ToPIN("041261FFFFFDACDE", "550000025321", PINBlockFormat.PIN_Block_Format.AnsiX98), "1234");
        }

        [Test]
        public void TestDieboldDecomposition()
        {
            Assert.AreEqual(PINBlockFormat.ToPIN("1234FFFFFFFFFFFF", "550000025321", PINBlockFormat.PIN_Block_Format.Diebold), "1234");
        }
    }
}
