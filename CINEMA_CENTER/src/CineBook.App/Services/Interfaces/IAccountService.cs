using CineBook.Models;

namespace CineBook.Services.Interfaces;

public interface IAccountService
{
    Task<AccountProfile> GetProfileAsync(CancellationToken ct);
    Task<AccountProfile> SaveAsync(AccountChanges changes, CancellationToken ct);
    Task<AccountProfile> UploadAvatarAsync(byte[] png, CancellationToken ct);
    Task<IReadOnlyList<AccountVoucher>> GetVouchersAsync(CancellationToken ct);
    Task VerifyPasswordAsync(string password, CancellationToken ct);
    Task ChangePasswordAsync(string current, string next, string confirmation, CancellationToken ct);
    Task SetInitialPasswordAsync(string next, string confirmation, CancellationToken ct);
}
