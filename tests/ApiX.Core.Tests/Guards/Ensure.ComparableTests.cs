using ApiX.Core.Guards;

namespace ApiX.Core.Tests.Guards;

public class Ensure_Comparable_Tests
{
    [Fact] public void GreaterThan_Int_Succeeds() => Ensure.GreaterThan(5, 3).ShouldBeValid();
    [Fact] public void GreaterThan_Int_Fails() => Ensure.GreaterThan(3, 3).ShouldBeInvalid("> 3");

    [Fact]
    public void GreaterOrEqual_Date_Succeeds()
        => Ensure.GreaterOrEqual(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-1)).ShouldBeValid();

    [Fact] public void LessThan_Int_Succeeds() => Ensure.LessThan(3, 5).ShouldBeValid();
    [Fact] public void LessOrEqual_Int_Succeeds() => Ensure.LessOrEqual(5, 5).ShouldBeValid();

    [Fact]
    public void Between_Date_Succeeds()
    {
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 12, 31);
        Ensure.Between(new DateTime(2025, 6, 1), start, end).ShouldBeValid();
    }

    [Fact]
    public void Between_Date_Fails()
    {
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 12, 31);
        Ensure.Between(new DateTime(2024, 12, 31), start, end).ShouldBeInvalid("between");
    }
}
