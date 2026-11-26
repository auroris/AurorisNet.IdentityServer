using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace IdentityServer;

internal sealed class OutstandingAuthorizationCodes : IDisposable
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> codes = new();
    private readonly TimeProvider clock;
    private readonly ITimer cleanup;

    public OutstandingAuthorizationCodes(TimeProvider clock)
    {
        this.clock = clock;
        var interval = TimeSpan.FromMinutes(1);
        cleanup = clock.CreateTimer(_ => RemoveExpired(), null, interval, interval);
    }

    // Store only a fingerprint of the opaque code OpenIddict actually returned.
    private static string Fingerprint(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    public bool TryAdd(string code, DateTimeOffset expiresAt) =>
        codes.TryAdd(Fingerprint(code), expiresAt);

    public bool TryRedeem(string code) =>
        codes.TryRemove(Fingerprint(code), out var expiresAt) && clock.GetUtcNow() <= expiresAt;

    internal void RemoveExpired()
    {
        var now = clock.GetUtcNow();
        foreach (var entry in codes)
        {
            if (entry.Value < now) codes.TryRemove(entry);
        }
    }

    public void Dispose() => cleanup.Dispose();
}

internal sealed class RegisterAuthorizationCode(OutstandingAuthorizationCodes codes) : IOpenIddictServerHandler<ProcessSignInContext>
{
    public ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.AuthorizationCode is not { Length: > 0 } code ||
            context.AuthorizationCodePrincipal?.GetExpirationDate() is not { } expiry ||
            !codes.TryAdd(code, expiry + context.Options.TokenValidationParameters.ClockSkew))
        {
            context.Reject(Errors.ServerError, "The authorization code could not be issued.");
        }

        return ValueTask.CompletedTask;
    }
}

internal sealed class RedeemAuthorizationCode(OutstandingAuthorizationCodes codes) : IOpenIddictServerHandler<ProcessSignInContext>
{
    public ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.EndpointType == OpenIddictServerEndpointType.Token && context.Request.IsAuthorizationCodeGrantType() &&
            (context.Request.Code is not { } code || !codes.TryRedeem(code)))
        {
            context.Reject(Errors.InvalidGrant, "The authorization code is no longer valid.");
        }

        return ValueTask.CompletedTask;
    }
}
