using ApiX.Core.Guards;

namespace ApiX.Core.Tests.Guards;

public class Ensure_EnumMisc_Tests
{
    private enum Role { User = 0, Admin = 1 }

    [Fact] public void EnumDefined_Succeeds() => Ensure.EnumDefined(Role.Admin).ShouldBeValid();
    [Fact] public void EnumDefined_Fails() => Ensure.EnumDefined((Role)42).ShouldBeInvalid("not defined");

    [Fact] public void ValidUri_Succeeds_Absolute() => Ensure.ValidUri("https://example.com").ShouldBeValid();
    [Fact] public void ValidUri_Fails() => Ensure.ValidUri("N O P E").ShouldBeInvalid("Invalid URI");

    [Fact] public void Satisfies_Succeeds() => Ensure.Satisfies(5, x => x % 5 == 0).ShouldBeValid();
    [Fact] public void Satisfies_Fails() => Ensure.Satisfies(6, x => x % 5 == 0).ShouldBeInvalid("Predicate");

    [Fact]
    public void And_ShortCircuits_To_First_Failure()
    {
        var r = Ensure.And(Ensure.NotNull("x"), Ensure.NotNull<string>(null), Ensure.NotNull("still-not-run"));
        r.ShouldBeInvalid();
    }

    [Fact]
    public void Or_Succeeds_When_Any_Valid()
    {
        var r = Ensure.Or(Ensure.NotNull<string>(null), Ensure.NotNull("x"));
        r.ShouldBeValid();
    }

    [Fact]
    public void Not_Inverts_Result()
    {
        Ensure.Not(CheckResult.Ok()).ShouldBeInvalid("Negation failed");
        Ensure.Not(CheckResult.Fail("x")).ShouldBeValid();
    }
}
