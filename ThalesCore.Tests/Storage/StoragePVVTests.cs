using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NUnit.Framework;
using ThalesCore.Storage;
using ThalesCore.PIN;

namespace ThalesCore.Tests.Storage
{
    [TestFixture]
    public class StoragePVVTests
    {
        [Test]
        public async Task Store_PersistedPVVOffset_VerifyPin()
        {
            var tmp = Path.Combine(TestContext.CurrentContext.WorkDirectory, "test_store_" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("THALES_STORE", "json");
            Environment.SetEnvironmentVariable("THALES_STORE_PATH", tmp);

            var store = StoreFactory.CreateFromEnvironment();
            await store.InitializeAsync();

            var key = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"; // 24-byte hex
            var pan = "400000123456"; // 12-digit account
            var pin = "1234";

            var pvv = PVV.ComputeVisaPVV(key, pan);
            var offset = PVV.ComputeIBM3624Offset(key, pan, pin);

            var protectedPvv = ProtectedData.Protect(Encoding.UTF8.GetBytes(pvv), null, DataProtectionScope.CurrentUser);
            var encPvvB64 = Convert.ToBase64String(protectedPvv);

            var protectedOffset = ProtectedData.Protect(Encoding.UTF8.GetBytes(offset), null, DataProtectionScope.CurrentUser);
            var encOffsetB64 = Convert.ToBase64String(protectedOffset);

            // Import the ZPK into the store (seed logic expects raw key bytes base64)
            byte[] keyBytes = new byte[key.Length / 2];
            for (int i = 0; i < keyBytes.Length; i++)
                keyBytes[i] = Convert.ToByte(key.Substring(i * 2, 2), 16);
            var keyRecord = new KeyRecord("ZPK_TEST_1", "ZPK", Convert.ToBase64String(keyBytes), "000000");
            await store.ImportKeyAsync(keyRecord);

            var acctId = "utacct1";
            var acc = new AccountRecord(acctId, pan, encPvvB64, encOffsetB64, 0, 0);
            await store.CreateOrUpdateAccountAsync(acc);

            var ok = await store.VerifyPinAsync(acctId, pin);
            Assert.IsTrue(ok, "VerifyPinAsync should succeed for stored PVV+offset");
        }
    }
}
