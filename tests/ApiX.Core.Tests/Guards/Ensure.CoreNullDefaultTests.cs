using ApiX.Core.Guards;

namespace ApiX.Core.Tests.Guards;

public class Ensure_CoreNullDefault_Tests
{
    [Fact] public void Null_Succeeds_For_Null() => Ensure.Null<string>(null).ShouldBeValid();
    [Fact] public void Null_Fails_For_Value() => Ensure.Null("x").ShouldBeInvalid("Expected null");

    [Fact] public void NotNull_Succeeds_For_Value() => Ensure.NotNull("x").ShouldBeValid();
    [Fact] public void NotNull_Fails_For_Null() => Ensure.NotNull<string>(null).ShouldBeInvalid("cannot be null");

    [Fact] public void NotDefault_Fails_For_Default_Int() => Ensure.NotDefault(default(int)).ShouldBeInvalid("default");
    [Fact] public void NotDefault_Succeeds_For_NonDefault_Int() => Ensure.NotDefault(1).ShouldBeValid();

    [Fact] public void GuidNotEmpty_Succeeds() => Ensure.GuidNotEmpty(Guid.NewGuid()).ShouldBeValid();
    [Fact] public void GuidNotEmpty_Fails_For_Empty() => Ensure.GuidNotEmpty(Guid.Empty).ShouldBeInvalid("Guid");
}
