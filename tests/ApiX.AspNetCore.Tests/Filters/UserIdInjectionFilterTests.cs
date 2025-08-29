using ApiX.Abstractions.Transport.Requests;
using ApiX.AspNetCore.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using FluentAssertions;

namespace ApiX.AspNetCore.Tests.Filters;

public class UserIdInjectionFilterTests
{
    private static ActionExecutingContext MakeContext(object argument, ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext();
        if (user is not null) httpContext.User = user;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        var actionArgs = new Dictionary<string, object?> { ["request"] = argument };

        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), actionArgs, controller: new object());
    }

    private static ActionExecutionDelegate NextDelegate() =>
        () => Task.FromResult<ActionExecutedContext>(
            new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary()),
                new List<IFilterMetadata>(),
                controller: new object()));

    [Fact]
    public async Task Sets_UserId_On_BaseRequest_When_Claim_Present()
    {
        var claimUserId = Guid.NewGuid().ToString();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("UserId", claimUserId) }, "test" ));

        var req = new TestBaseRequest();
        var ctx = MakeContext(req, principal);
        var filter = new UserIdInjectionFilter();

        await filter.OnActionExecutionAsync(ctx, NextDelegate());

        req.UserId.Should().Be(Guid.Parse(claimUserId));
        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task Forbids_When_Required_And_Missing()
    {
        var req = new TestBaseRequest();
        var ctx = MakeContext(req, user: new ClaimsPrincipal(new ClaimsIdentity())); // no claims
        var filter = new UserIdInjectionFilter(required: true);

        await filter.OnActionExecutionAsync(ctx, NextDelegate());

        ctx.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Does_Not_Forbid_When_Not_Required_And_Missing()
    {
        var req = new TestBaseRequest();
        var ctx = MakeContext(req, user: new ClaimsPrincipal(new ClaimsIdentity())); // no claims
        var filter = new UserIdInjectionFilter(required: false);

        await filter.OnActionExecutionAsync(ctx, NextDelegate());

        ctx.Result.Should().BeNull(); // proceeds
        req.UserId.Should().Be(Guid.Empty);
    }

    private sealed class TestBaseRequest : BaseRequest { }
}
