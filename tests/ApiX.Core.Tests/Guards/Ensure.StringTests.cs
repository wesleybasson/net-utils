using ApiX.Core.Guards;
using System.Text.RegularExpressions;

namespace ApiX.Core.Tests.Guards;

public class Ensure_String_Tests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NotNullOrEmpty_Fails(string? s) => Ensure.NotNullOrEmpty(s).ShouldBeInvalid("null or empty");

    [Fact] public void NotNullOrEmpty_Succeeds() => Ensure.NotNullOrEmpty("ok").ShouldBeValid();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotNullOrWhiteSpace_Fails(string? s) => Ensure.NotNullOrWhiteSpace(s).ShouldBeInvalid("whitespace");

    [Fact] public void LengthBetween_Succeeds_In_Range() => Ensure.LengthBetween("hello", 3, 5).ShouldBeValid();
    [Fact] public void LengthBetween_Fails_Too_Short() => Ensure.LengthBetween("hi", 3, 5).ShouldBeInvalid("between 3 and 5");
    [Fact] public void LengthBetween_Fails_Null() => Ensure.LengthBetween(null, 1, 2).ShouldBeInvalid("cannot be null");

    [Fact] public void Matches_Succeeds() => Ensure.Matches("abc123", new Regex(@"^[a-z]+\d+$")).ShouldBeValid();
    [Fact] public void Matches_Fails() => Ensure.Matches("!!!", new Regex(@"^[a-z]+\d+$")).ShouldBeInvalid("match");
}
