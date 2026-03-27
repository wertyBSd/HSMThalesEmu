using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ThalesCore.Storage
{
    public static class SeedData
    {
        public static async Task EnsureSeedAsync(IKeyStore store)
        {
            // Always initialize underlying store first
            await store.InitializeAsync();

            // If test key exists, assume seeded
            var existing = await store.GetKeyAsync("ZPK_TEST_1");
            if (existing != null) return;

            // For JSON/test scenarios use a deterministic key so test data is stable and
            // the repository can include a seeded JSON store. Derive 16 bytes from a
            // fixed label via SHA256 to get reproducible key material.
            byte[] keyBytes;
            var storeType = Environment.GetEnvironmentVariable("THALES_STORE")?.ToLowerInvariant() ?? "json";
            if (storeType == "json")
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                var src = Encoding.UTF8.GetBytes("STATIC_TEST_ZPK_0001");
                var hash = sha.ComputeHash(src);
                keyBytes = new byte[16];
                Array.Copy(hash, 0, keyBytes, 0, 16);
            }
            else
            {
                // non-json stores can use random keys
                keyBytes = new byte[16];
                RandomNumberGenerator.Fill(keyBytes);
            }

            // Compute KCV (encrypt 8 zero bytes with 3DES ECB, take first 6 hex)
            string kcvHex;
            using (var tdes = TripleDES.Create())
            {
                tdes.Key = keyBytes;
                tdes.Mode = System.Security.Cryptography.CipherMode.ECB;
                tdes.Padding = PaddingMode.None;
                var enc = tdes.CreateEncryptor().TransformFinalBlock(new byte[8], 0, 8);
                kcvHex = BitConverter.ToString(enc).Replace("-", "").Substring(0, 6);
            }

            var keyRecord = new KeyRecord(
                Id: "ZPK_TEST_1",
                KeyType: "ZPK",
                EncryptedKeyBase64: Convert.ToBase64String(keyBytes),
                Kcv: kcvHex
            );
            await store.ImportKeyAsync(keyRecord);

            // Create one or several test accounts
            // seed account using PVV/offset. Use ZPK_TEST_1 for PVV derivation
            var zpk = await store.GetKeyAsync("ZPK_TEST_1");
            string zpkHex = null;
            if (zpk != null)
            {
                var zpkBytes = Convert.FromBase64String(zpk.EncryptedKeyBase64);
                zpkHex = BitConverter.ToString(zpkBytes).Replace("-", "");
            }

            var plainPin = "1234";
            string? encPvvB64 = null;
            string? encOffsetB64 = null;
            if (!string.IsNullOrEmpty(zpkHex))
            {
                var pvv = ThalesCore.PIN.PVV.ComputeVisaPVV(zpkHex, "4000000000000002");
                var offset = ThalesCore.PIN.PVV.ComputeIBM3624Offset(zpkHex, "4000000000000002", plainPin);
                var pvvBytes = Encoding.UTF8.GetBytes(pvv);
                var offsBytes = Encoding.UTF8.GetBytes(offset);
                if (storeType == "json")
                {
                    // For JSON test stores, store unprotected base64 so the data is portable
                    encPvvB64 = Convert.ToBase64String(pvvBytes);
                    encOffsetB64 = Convert.ToBase64String(offsBytes);
                }
                else
                {
                    encPvvB64 = Convert.ToBase64String(ProtectedData.Protect(pvvBytes, null, DataProtectionScope.CurrentUser));
                    encOffsetB64 = Convert.ToBase64String(ProtectedData.Protect(offsBytes, null, DataProtectionScope.CurrentUser));
                }
            }
            else
            {
                // fallback: store plain PIN protected (legacy behaviour)
                var pvvBytes = Encoding.UTF8.GetBytes(plainPin);
                encPvvB64 = storeType == "json" ? Convert.ToBase64String(pvvBytes) : Convert.ToBase64String(ProtectedData.Protect(pvvBytes, null, DataProtectionScope.CurrentUser));
            }

            var account = new AccountRecord(
                AccountId: "acct-0001",
                Pan: "4000000000000002",
                EncryptedPvvBase64: encPvvB64 ?? string.Empty,
                EncryptedOffsetBase64: encOffsetB64,
                FailedAttempts: 0,
                LockedUntilUtc: 0
            );
            await store.CreateOrUpdateAccountAsync(account);

            // If using JSON store, add extra test data for debugging
            if (storeType == "json")
            {
                for (int i = 2; i <= 5; i++)
                {
                    var aid = $"acct-000{i}";
                    var pan = $"40000000000000{10 + i:D2}";
                    var plain = (1000 + i).ToString().Substring(1).PadLeft(4, '0');
                    string? encP = null;
                    string? encOff = null;
                    if (!string.IsNullOrEmpty(zpkHex))
                    {
                        var pvv = ThalesCore.PIN.PVV.ComputeVisaPVV(zpkHex, pan);
                        var offs = ThalesCore.PIN.PVV.ComputeIBM3624Offset(zpkHex, pan, plain);
                        if (storeType == "json")
                        {
                            encP = Convert.ToBase64String(Encoding.UTF8.GetBytes(pvv));
                            encOff = Convert.ToBase64String(Encoding.UTF8.GetBytes(offs));
                        }
                        else
                        {
                            encP = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(pvv), null, DataProtectionScope.CurrentUser));
                            encOff = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(offs), null, DataProtectionScope.CurrentUser));
                        }
                    }
                    else
                    {
                        encP = storeType == "json" ? Convert.ToBase64String(Encoding.UTF8.GetBytes(plain)) : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser));
                    }
                    var acc = new AccountRecord(aid, pan, encP ?? string.Empty, encOff, 0, 0);
                    await store.CreateOrUpdateAccountAsync(acc);
                }
            }

            await store.AddAuditAsync(new AuditRecord(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "Seed", "ZPK_TEST_1", "OK"));
        }
    }
}
