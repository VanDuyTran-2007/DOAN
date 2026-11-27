using CineBook.Models;

namespace CineBook.Services.Interfaces;

public interface ISocialAuthService
{
    Task<AuthResult> AuthenticateAsync(string provider, bool link, CancellationToken ct);
}
