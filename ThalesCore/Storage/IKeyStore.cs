using System.Threading.Tasks;

namespace ThalesCore.Storage
{
    public record KeyRecord(string Id, string KeyType, string EncryptedKeyBase64, string Kcv);
    public record AccountRecord(string AccountId, string Pan, string EncryptedPvvBase64, string? EncryptedOffsetBase64, int FailedAttempts, long LockedUntilUtc);
    public record AuditRecord(long TimestampUtc, string Operation, string AccountId, string Result);

    public interface IKeyStore
    {
        Task InitializeAsync();
        Task ImportKeyAsync(KeyRecord key);
        Task<KeyRecord?> GetKeyAsync(string id);
        Task CreateOrUpdateAccountAsync(AccountRecord account);
        Task<AccountRecord?> GetAccountAsync(string accountId);
        Task<bool> VerifyPinAsync(string accountId, string pinPlain);
        Task AddAuditAsync(AuditRecord audit);
    }
}
