using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;

namespace ThalesCore.Storage
{
    public class SqliteKeyStore : IKeyStore
    {
        private readonly string _dbPath;
        private readonly string _connString;

        public SqliteKeyStore(string dbPath)
        {
            _dbPath = dbPath;
            _connString = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
        }

        public async Task InitializeAsync()
        {
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using var conn = new SqliteConnection(_connString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS keys(id TEXT PRIMARY KEY, keyType TEXT, encryptedKey BLOB, kcv TEXT);
CREATE TABLE IF NOT EXISTS accounts(accountId TEXT PRIMARY KEY, pan TEXT, encryptedPvv BLOB, encryptedOffset BLOB, failedAttempts INTEGER, lockedUntilUtc INTEGER);
CREATE TABLE IF NOT EXISTS audit(ts INTEGER, operation TEXT, accountId TEXT, result TEXT);
";
            await cmd.ExecuteNonQueryAsync();
        }

        private static byte[] Protect(byte[] data)
        {
            try { return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser); }
            catch { return data; }
        }
        private static byte[] Unprotect(byte[] data)
        {
            try { return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser); }
            catch { return data; }
        }

        public async Task ImportKeyAsync(KeyRecord key)
        {
            using var conn = new SqliteConnection(_connString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO keys(id,keyType,encryptedKey,kcv) VALUES($id,$type,$enc,$kcv)";
            cmd.Parameters.AddWithValue("$id", key.Id);
            cmd.Parameters.AddWithValue("$type", key.KeyType);
            var enc = Convert.FromBase64String(key.EncryptedKeyBase64);
            cmd.Parameters.AddWithValue("$enc", Protect(enc));
            cmd.Parameters.AddWithValue("$kcv", key.Kcv);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<KeyRecord?> GetKeyAsync(string id)
        {
            using var conn = new SqliteConnection(_connString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id,keyType,encryptedKey,kcv FROM keys WHERE id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            var enc = (byte[])r[2];
            var dec = Unprotect(enc);
            return new KeyRecord(r.GetString(0), r.GetString(1), Convert.ToBase64String(dec), r.GetString(3));
        }

        public async Task CreateOrUpdateAccountAsync(AccountRecord account)
        {
            using var conn = new SqliteConnection(_connString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO accounts(accountId,pan,encryptedPvv,encryptedOffset,failedAttempts,lockedUntilUtc) VALUES($id,$pan,$enc,$off,$fa,$lu)";
            cmd.Parameters.AddWithValue("$id", account.AccountId);
            cmd.Parameters.AddWithValue("$pan", account.Pan);
            cmd.Parameters.AddWithValue("$enc", Protect(Convert.FromBase64String(account.EncryptedPvvBase64)));
            cmd.Parameters.AddWithValue("$off", account.EncryptedOffsetBase64 == null ? DBNull.Value : (object)Protect(Convert.FromBase64String(account.EncryptedOffsetBase64)));
            cmd.Parameters.AddWithValue("$fa", account.FailedAttempts);
            cmd.Parameters.AddWithValue("$lu", account.LockedUntilUtc);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<AccountRecord?> GetAccountAsync(string accountId)
        {
            using var conn = new SqliteConnection(_connString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT accountId,pan,encryptedPvv,encryptedOffset,failedAttempts,lockedUntilUtc FROM accounts WHERE accountId=$id";
            cmd.Parameters.AddWithValue("$id", accountId);
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            var enc = (byte[])r[2];
            var dec = Unprotect(enc);
            string? encOffsetB64 = null;
            if (!r.IsDBNull(3))
            {
                var encOff = (byte[])r[3];
                var decOff = Unprotect(encOff);
                encOffsetB64 = Convert.ToBase64String(decOff);
            }
            return new AccountRecord(r.GetString(0), r.GetString(1), Convert.ToBase64String(dec), encOffsetB64, r.GetInt32(4), r.GetInt64(5));
        }

        public async Task<bool> VerifyPinAsync(string accountId, string pinPlain)
        {
            var acc = await GetAccountAsync(accountId);
            if (acc == null) return false;

            // If offset present, use PVV natural + offset to reconstruct PIN
            if (!string.IsNullOrEmpty(acc.EncryptedOffsetBase64))
            {
                var key = await GetKeyAsync("ZPK_TEST_1");
                if (key == null) return false;
                var keyBytes = Convert.FromBase64String(key.EncryptedKeyBase64);
                var keyHex = BitConverter.ToString(keyBytes).Replace("-", "");
                var natural = ThalesCore.PIN.PVV.ComputeVisaPVV(keyHex, acc.Pan).Substring(0, 4);
                var offset = Encoding.UTF8.GetString(Unprotect(Convert.FromBase64String(acc.EncryptedOffsetBase64)));
                var reconstructed = string.Concat(System.Linq.Enumerable.Range(0, 4).Select(i => (char)('0' + (((natural[i] - '0') + (offset[i] - '0')) % 10))));
                var ok = reconstructed == pinPlain;
                var updated = acc with { FailedAttempts = ok ? 0 : acc.FailedAttempts + 1 };
                await CreateOrUpdateAccountAsync(updated);
                await AddAuditAsync(new AuditRecord(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "VerifyPin", accountId, ok ? "OK" : "FAIL"));
                return ok;
            }

            // fallback: legacy stored PIN/PVV
            var stored = Encoding.UTF8.GetString(Unprotect(Convert.FromBase64String(acc.EncryptedPvvBase64)));
            var ok2 = stored == pinPlain;
            var updated2 = acc with { FailedAttempts = ok2 ? 0 : acc.FailedAttempts + 1 };
            await CreateOrUpdateAccountAsync(updated2);
            await AddAuditAsync(new AuditRecord(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "VerifyPin", accountId, ok2 ? "OK" : "FAIL"));
            return ok2;
        }

        public async Task AddAuditAsync(AuditRecord audit)
        {
            using var conn = new SqliteConnection(_connString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO audit(ts,operation,accountId,result) VALUES($ts,$op,$id,$res)";
            cmd.Parameters.AddWithValue("$ts", audit.TimestampUtc);
            cmd.Parameters.AddWithValue("$op", audit.Operation);
            cmd.Parameters.AddWithValue("$id", audit.AccountId);
            cmd.Parameters.AddWithValue("$res", audit.Result);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
