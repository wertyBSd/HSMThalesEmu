using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace ThalesCore.Storage
{
    public class JsonKeyStore : IKeyStore
    {
        private readonly string _dir;
        private readonly string _keysFile;
        private readonly string _accountsFile;
        private readonly string _auditFile;

        public JsonKeyStore(string directory)
        {
            _dir = directory;
            _keysFile = Path.Combine(_dir, "keys.json");
            _accountsFile = Path.Combine(_dir, "accounts.json");
            _auditFile = Path.Combine(_dir, "audit.json");
        }

        public Task InitializeAsync()
        {
            Directory.CreateDirectory(_dir);
            if (!File.Exists(_keysFile)) File.WriteAllText(_keysFile, "[]");
            if (!File.Exists(_accountsFile)) File.WriteAllText(_accountsFile, "[]");
            if (!File.Exists(_auditFile)) File.WriteAllText(_auditFile, "[]");
            return Task.CompletedTask;
        }

        private static byte[] Protect(byte[] data)
        {
            try
            {
                return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            }
            catch
            {
                return data;
            }
        }

        private static byte[] Unprotect(byte[] data)
        {
            try
            {
                return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            }
            catch
            {
                return data;
            }
        }

        public async Task ImportKeyAsync(KeyRecord key)
        {
            var raw = await ReadAllTextWithRetriesAsync(_keysFile);
            var keys = JsonSerializer.Deserialize<List<KeyRecord>>(raw) ?? new();
            keys.RemoveAll(k => k.Id == key.Id);
            keys.Add(key);
            var serialized = JsonSerializer.Serialize(keys);
            await WriteAllTextWithRetriesAsync(_keysFile, serialized);
        }

        public async Task<KeyRecord?> GetKeyAsync(string id)
        {
            var raw = await ReadAllTextWithRetriesAsync(_keysFile);
            var keys = JsonSerializer.Deserialize<List<KeyRecord>>(raw) ?? new();
            return keys.FirstOrDefault(k => k.Id == id);
        }

        public async Task CreateOrUpdateAccountAsync(AccountRecord account)
        {
            Console.WriteLine("JsonKeyStore: CreateOrUpdateAccountAsync writing to " + _accountsFile + " account=" + account.AccountId);
            var raw = await ReadAllTextWithRetriesAsync(_accountsFile);
            var accounts = JsonSerializer.Deserialize<List<AccountRecord>>(raw) ?? new();
            accounts.RemoveAll(a => a.AccountId == account.AccountId);
            accounts.Add(account);
            var serialized = JsonSerializer.Serialize(accounts);
            await WriteAllTextWithRetriesAsync(_accountsFile, serialized);
            Console.WriteLine("JsonKeyStore: CreateOrUpdateAccountAsync wrote " + serialized.Length + " bytes to " + _accountsFile);
        }

        public async Task<AccountRecord?> GetAccountAsync(string accountId)
        {
            Console.WriteLine("JsonKeyStore: GetAccountAsync reading " + _accountsFile + " for account=" + accountId);
            var raw = await ReadAllTextWithRetriesAsync(_accountsFile);
            Console.WriteLine("JsonKeyStore: GetAccountAsync file len=" + (raw?.Length ?? 0));
            var accounts = JsonSerializer.Deserialize<List<AccountRecord>>(raw) ?? new();
            var found = accounts.FirstOrDefault(a => a.AccountId == accountId);
            Console.WriteLine("JsonKeyStore: GetAccountAsync found=" + (found != null));
            return found;
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
                var updated = acc with
                {
                    FailedAttempts = ok ? 0 : acc.FailedAttempts + 1
                };
                await CreateOrUpdateAccountAsync(updated);
                await AddAuditAsync(new AuditRecord(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "VerifyPin", accountId, ok ? "OK" : "FAIL"));
                return ok;
            }

            // fallback: legacy stored PIN/PVV
            var pvvBytes = Convert.FromBase64String(acc.EncryptedPvvBase64);
            var stored = Encoding.UTF8.GetString(Unprotect(pvvBytes));
            var ok2 = stored == pinPlain;
            var updated2 = acc with
            {
                FailedAttempts = ok2 ? 0 : acc.FailedAttempts + 1
            };
            await CreateOrUpdateAccountAsync(updated2);
            await AddAuditAsync(new AuditRecord(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "VerifyPin", accountId, ok2 ? "OK" : "FAIL"));
            return ok2;
        }

        public async Task AddAuditAsync(AuditRecord audit)
        {
            var raw = await ReadAllTextWithRetriesAsync(_auditFile);
            var list = JsonSerializer.Deserialize<List<AuditRecord>>(raw) ?? new();
            list.Add(audit);
            var serialized = JsonSerializer.Serialize(list);
            await WriteAllTextWithRetriesAsync(_auditFile, serialized);
        }

        private async Task<string> ReadAllTextWithRetriesAsync(string path)
        {
            const int maxAttempts = 6;
            int attempt = 0;
            int delay = 20; // ms
            while (true)
            {
                try
                {
                    // Allow other processes to write while we read
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs, Encoding.UTF8);
                    return await sr.ReadToEndAsync();
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    attempt++;
                    await Task.Delay(delay);
                    delay *= 2;
                    continue;
                }
                catch (UnauthorizedAccessException) when (attempt < maxAttempts)
                {
                    attempt++;
                    await Task.Delay(delay);
                    delay *= 2;
                    continue;
                }
            }
        }

        private async Task WriteAllTextWithRetriesAsync(string path, string content)
        {
            const int maxAttempts = 6;
            int attempt = 0;
            int delay = 20; // ms
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
            while (true)
            {
                try
                {
                    var temp = path + ".tmp";
                    using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        await sw.WriteAsync(content);
                        await sw.FlushAsync();
                    }
                    // Atomic-ish replace
                    File.Copy(temp, path, true);
                    File.Delete(temp);
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    attempt++;
                    await Task.Delay(delay);
                    delay *= 2;
                    continue;
                }
                catch (UnauthorizedAccessException) when (attempt < maxAttempts)
                {
                    attempt++;
                    await Task.Delay(delay);
                    delay *= 2;
                    continue;
                }
            }
        }
    }
}
