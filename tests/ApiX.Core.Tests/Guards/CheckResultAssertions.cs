using ApiX.Core.Guards;

namespace ApiX.Core.Tests.Guards;

internal static class CheckResultAssertions
{
    public static void ShouldBeValid(this CheckResult r)
        => Assert.True(r.IsValid, r.Reason ?? "Expected valid CheckResult.");

    public static void ShouldBeInvalid(this CheckResult r, string? reasonContains = null)
    {
        Assert.False(r.IsValid, "Expected invalid CheckResult.");
        if (reasonContains is not null)
            Assert.Contains(reasonContains, r.Reason ?? "");
    }
}
