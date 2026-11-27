using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Data;

public sealed class AccountPasswords(IPasswordHasher<User> hasher)
{
    internal string Hash(User user, string password) => hasher.HashPassword(user, password);

    internal bool Verify(User user, string password, out bool needsUpgrade)
    {
        needsUpgrade = false;
        var stored = user.PasswordHash;
        if (stored.Length == 64 && stored.All(Uri.IsHexDigit))
        {
            var legacy = SHA256.HashData(Encoding.UTF8.GetBytes("CineBookSalt2026:" + password));
            needsUpgrade = CryptographicOperations.FixedTimeEquals(legacy, Convert.FromHexString(stored));
            return needsUpgrade;
        }
        if (stored.Length == 0) return false;

        try
        {
            var result = hasher.VerifyHashedPassword(user, stored, password);
            needsUpgrade = result == PasswordVerificationResult.SuccessRehashNeeded;
            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    internal async Task UpgradeStoredPlaintextAsync(CineBookDbContext db, CancellationToken ct)
    {
        // Chỉ dùng lúc cập nhật database cũ, không dùng để xác thực đăng nhập.
        const string legacyPrefix = "PLAIN:";
        var users = await db.Users.Where(user => user.PasswordHash.StartsWith(legacyPrefix)).ToListAsync(ct);
        foreach (var user in users)
            user.PasswordHash = Hash(user, user.PasswordHash[legacyPrefix.Length..]);
        await db.SaveChangesAsync(ct);
    }
}
