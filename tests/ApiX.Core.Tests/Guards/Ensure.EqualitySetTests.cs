using ApiX.Core.Guards;

namespace ApiX.Core.Tests.Guards;

public class Ensure_EqualitySet_Tests
{
    [Fact] public void Equal_Succeeds_DefaultComparer() => Ensure.Equal(5, 5).ShouldBeValid();
    [Fact] public void Equal_Fails_DefaultComparer() => Ensure.Equal(5, 6).ShouldBeInvalid("not equal");

    [Fact] public void NotEqual_Succeeds() => Ensure.NotEqual(5, 6).ShouldBeValid();
    [Fact] public void NotEqual_Fails() => Ensure.NotEqual(5, 5).ShouldBeInvalid("must not be equal");

    [Fact]
    public void Equal_With_CustomComparer_Succeeds()
        => Ensure.Equal("a", "A", StringComparer.OrdinalIgnoreCase).ShouldBeValid();

    [Fact]
    public void OneOf_Succeeds()
        => Ensure.OneOf("Admin", new[] { "Admin", "User" }).ShouldBeValid();

    [Fact]
    public void OneOf_Fails()
        => Ensure.OneOf("Guest", new[] { "Admin", "User" }).ShouldBeInvalid("allowed");

    [Fact]
    public void NotOneOf_Succeeds()
        => Ensure.NotOneOf("Guest", new[] { "Admin", "User" }).ShouldBeValid();

    [Fact]
    public void NotOneOf_Fails()
        => Ensure.NotOneOf("Admin", new[] { "Admin", "User" }).ShouldBeInvalid("disallowed");
}
