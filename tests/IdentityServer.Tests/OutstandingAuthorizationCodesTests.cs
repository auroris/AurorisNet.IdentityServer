namespace IdentityServer.Tests;

public sealed class OutstandingAuthorizationCodesTests
{
    [Fact]
    public void OnlyOneCallerCanConsumeAnOutstandingEntry()
    {
        using var codes = new OutstandingAuthorizationCodes(TimeProvider.System);
        using var otherIssuer = new OutstandingAuthorizationCodes(TimeProvider.System);
        Assert.True(codes.TryAdd("sample", DateTimeOffset.UtcNow.AddMinutes(5)));
        Assert.False(otherIssuer.TryRedeem("sample"));

        int successes = 0;
        Parallel.For(0, 32, _ =>
        {
            if (codes.TryRedeem("sample")) Interlocked.Increment(ref successes);
        });
        Assert.Equal(1, successes);
    }

    [Fact]
    public void ExpiryIsEnforcedBetweenSweepsAndCleanupKeepsLiveEntries()
    {
        using var codes = new OutstandingAuthorizationCodes(TimeProvider.System);
        var past = DateTimeOffset.UtcNow.AddMinutes(-5);
        var future = DateTimeOffset.UtcNow.AddMinutes(5);
        Assert.True(codes.TryAdd("expired", past));
        Assert.False(codes.TryRedeem("expired"));

        Assert.True(codes.TryAdd("abandoned", past));
        Assert.True(codes.TryAdd("active", future));
        codes.RemoveExpired();
        Assert.True(codes.TryAdd("abandoned", future));
        Assert.True(codes.TryRedeem("active"));
    }
}
