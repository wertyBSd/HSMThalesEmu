using System.Linq;
using NUnit.Framework;

namespace ThalesCore.Tests.PIN
{
    [TestFixture]
    public class PVVTests
    {
        [Test]
        public void VisaPVV_Returns4Digits()
        {
            var key = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"; // 24-byte (48 hex)
            var pan = "4000001234567890";
            var pvv = ThalesCore.PIN.PVV.ComputeVisaPVV(key, pan);
            Assert.That(pvv, Is.Not.Null.And.Length.EqualTo(4));
            Assert.That(pvv.All(char.IsDigit));
        }

        [Test]
        public void IBM3624_OffsetRoundTrip()
        {
            var key = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"; // 24-byte (48 hex)
            var pan = "4000001234567890";
            var pin = "1234";
            var offset = ThalesCore.PIN.PVV.ComputeIBM3624Offset(key, pan, pin);
            Assert.That(offset, Is.Not.Null.And.Length.EqualTo(4));

            // Verify that applying the offset to the natural reference yields the PIN
            var natural = ThalesCore.PIN.PVV.ComputeVisaPVV(key, pan).Substring(0, 4);
            var reconstructed = string.Concat(Enumerable.Range(0, 4).Select(i => ((natural[i] - '0') + (offset[i] - '0')) % 10));
            Assert.That(reconstructed, Is.EqualTo(pin));
        }
    }
}
