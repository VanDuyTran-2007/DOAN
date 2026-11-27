using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace CineBook.Api.Controllers;

public sealed class ExternalAuthFlows : IDisposable
{
    private readonly MemoryCache _flows = new(new MemoryCacheOptions { SizeLimit = 512, CompactionPercentage = 0 });
    internal const string FlowKey = "cinebook-flow";

    internal Guid? Create(string provider, int port, string challenge, string? owner)
    {
        var id = Guid.NewGuid();
        _flows.Set(id, new Flow(provider, port, challenge, owner),
            new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        return _flows.TryGetValue(id, out _) ? id : null;
    }

    internal string? Begin(Guid id)
    {
        if (!_flows.TryGetValue<Flow>(id, out var flow) || flow is null) return null;
        lock (flow)
        {
            if (flow.Started) return null;
            flow.Started = true;
            return flow.Provider;
        }
    }

    internal string? BrowserCallback(AuthenticationProperties? properties, ClaimsPrincipal? principal)
    {
        if (properties is null || !properties.Items.TryGetValue(FlowKey, out var key) ||
            !Guid.TryParseExact(key, "N", out var id) ||
            !_flows.TryGetValue<Flow>(id, out var flow) || flow is null) return null;

        lock (flow)
        {
            if (!flow.Started || flow.Code is not null) return null;
            var callback = $"http://127.0.0.1:{flow.Port}/cinebook-auth/?flow={id:N}";
            var subject = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (subject is not { Length: > 0 and <= 255 })
            {
                _flows.Remove(id);
                return callback + "&error=cancelled";
            }
            flow.Profile = new ExternalProfile(flow.Provider, subject, principal!.FindFirstValue(ClaimTypes.Name),
                principal.FindFirstValue(ClaimTypes.Email));
            flow.Code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            return callback + "&code=" + flow.Code;
        }
    }

    internal ExternalProfile? Redeem(Guid id, string code, string verifier, string? owner)
    {
        if (!_flows.TryGetValue<Flow>(id, out var flow) || flow is null) return null;
        lock (flow)
        {
            var proof = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            if (flow.Profile is null || flow.Code is null || flow.Owner is not null && flow.Owner != owner ||
                !FixedEquals(flow.Challenge, proof) || !FixedEquals(flow.Code, code))
                return null;

            var result = flow.Profile with { Owner = flow.Owner };
            flow.Profile = null;
            _flows.Remove(id);
            return result;
        }
    }

    private static bool FixedEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));

    public void Dispose() => _flows.Dispose();

    private sealed class Flow(string provider, int port, string challenge, string? owner)
    {
        public string Provider { get; } = provider;
        public int Port { get; } = port;
        public string Challenge { get; } = challenge;
        public string? Owner { get; } = owner;
        public bool Started { get; set; }
        public string? Code { get; set; }
        public ExternalProfile? Profile { get; set; }
    }
}

internal sealed record ExternalProfile(string Provider, string Subject, string? FullName, string? Email, string? Owner = null);
