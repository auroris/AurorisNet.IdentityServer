using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Logging;
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

internal sealed class RegisterAuthorizationCode(OutstandingAuthorizationCodes codes, ILogger<RegisterAuthorizationCode> logger)
    : IOpenIddictServerHandler<ProcessSignInContext>
{
    public ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.AuthorizationCode is not { Length: > 0 } code ||
            context.AuthorizationCodePrincipal?.GetExpirationDate() is not { } expiry ||
            !codes.TryAdd(code, expiry + context.Options.TokenValidationParameters.ClockSkew))
        {
            // A code this table cannot track could be exchanged more than once, so it is never returned.
            logger.LogError("Authorization code could not be recorded as outstanding for client '{ClientId}', so none was issued and the application receives server_error. This is a server fault: the new code had no value or no expiry, or it was already recorded, which happens when IdentityServer.Add registers the single-use handlers twice.",
                context.Request.ClientId);
            context.Reject(Errors.ServerError, "The authorization code could not be issued.");
        }

        return ValueTask.CompletedTask;
    }
}

internal sealed class RedeemAuthorizationCode(OutstandingAuthorizationCodes codes, ILogger<RedeemAuthorizationCode> logger)
    : IOpenIddictServerHandler<ProcessSignInContext>
{
    public ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.EndpointType == OpenIddictServerEndpointType.Token && context.Request.IsAuthorizationCodeGrantType() &&
            (context.Request.Code is not { } code || !codes.TryRedeem(code)))
        {
            // OpenIddict has already validated the code, so only its place in this process's table is in doubt.
            // Record who presented it and whose sign-in it carries; never log the code.
            logger.LogWarning("Authorization code refused for client '{ClientId}' from {RemoteIp}, issued for subject {Subject}: it is not outstanding in this server process, so no tokens were issued. The code was already exchanged, or this process did not issue it (the server restarted, or a second process serves the same issuer). An application that repeats a token request causes this, and so does a replayed code.",
                context.Request.ClientId,
                context.Transaction.GetHttpRequest()?.HttpContext.Connection.RemoteIpAddress,
                context.Principal?.GetClaim(Claims.Subject) ?? "(unknown)");
            context.Reject(Errors.InvalidGrant, "The authorization code is no longer valid.");
        }

        return ValueTask.CompletedTask;
    }
}
