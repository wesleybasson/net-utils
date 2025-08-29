using ApiX.Core.Guards;

namespace ApiX.Core.Tests.Guards;

public class Ensure_Collection_Tests
{
    [Fact] public void Empty_Succeeds_For_Null() => Ensure.Empty<int>(null).ShouldBeValid();
    [Fact] public void Empty_Succeeds_For_Empty() => Ensure.Empty(new int[0]).ShouldBeValid();
    [Fact] public void Empty_Fails_For_Items() => Ensure.Empty(new[] { 1 }).ShouldBeInvalid("empty");

    [Fact] public void NotEmpty_Fails_For_Null() => Ensure.NotEmpty<int>(null).ShouldBeInvalid("null or empty");
    [Fact] public void NotEmpty_Fails_For_Empty() => Ensure.NotEmpty(new int[0]).ShouldBeInvalid("null or empty");
    [Fact] public void NotEmpty_Succeeds_For_Items() => Ensure.NotEmpty(new[] { 1 }).ShouldBeValid();

    [Fact] public void Any_NoPredicate_Succeeds() => Ensure.Any(new[] { 1 }).ShouldBeValid();
    [Fact] public void Any_NoPredicate_Fails_Empty() => Ensure.Any(new int[0]).ShouldBeInvalid("no matching");
    [Fact] public void Any_WithPredicate_Succeeds() => Ensure.Any(new[] { 1, 2, 3 }, x => x > 2).ShouldBeValid();
    [Fact] public void None_WithPredicate_Succeeds() => Ensure.None(new[] { 1, 2 }, x => x > 5).ShouldBeValid();
    [Fact] public void None_WithPredicate_Fails() => Ensure.None(new[] { 1, 2 }, x => x >= 2).ShouldBeInvalid("disallowed");

    [Fact] public void All_Succeeds() => Ensure.All(new[] { 2, 4, 6 }, x => x % 2 == 0).ShouldBeValid();
    [Fact] public void All_Fails() => Ensure.All(new[] { 2, 3, 6 }, x => x % 2 == 0).ShouldBeInvalid("predicate");

    [Fact] public void CountAtLeast_Succeeds() => Ensure.CountAtLeast(new[] { 1, 2 }, 2).ShouldBeValid();
    [Fact] public void CountAtMost_Succeeds() => Ensure.CountAtMost(new[] { 1, 2 }, 2).ShouldBeValid();
    [Fact] public void CountBetween_Succeeds() => Ensure.CountBetween(new[] { 1, 2, 3 }, 1, 3).ShouldBeValid();

    [Fact]
    public void UniqueBy_Fails_On_Duplicate_Key()
        => Ensure.UniqueBy(new[] { "a", "b", "a" }, s => s).ShouldBeInvalid("duplicate");

    [Fact]
    public void UniqueBy_Succeeds()
        => Ensure.UniqueBy(new[] { "a", "bb", "ccc" }, s => s.Length).ShouldBeValid();
}
