using ApiX.Abstractions.Transport.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiX.AspNetCore.Filters;

/// <summary>
/// An ASP.NET Core action filter that injects a <c>UserId</c> value from the authenticated user's claims
/// into any action arguments of type <see cref="BaseRequest"/>.
/// </summary>
/// <remarks>
/// <para>
/// The filter inspects the current <see cref="System.Security.Claims.ClaimsPrincipal"/> for a claim
/// matching the specified claim type (defaults to <c>"UserId"</c>).
/// </para>
/// <para>
/// If the claim is found and parsed as a <see cref="Guid"/>, its value is assigned to the
/// <see cref="BaseRequest.UserId"/> property (if it is currently <see cref="Guid.Empty"/>).
/// </para>
/// <para>
/// If the claim is required but not present or invalid, the request is short-circuited with a <see cref="ForbidResult"/>.
/// </para>
/// </remarks>
public sealed class UserIdInjectionFilter : IAsyncActionFilter
{
    private readonly string _claimType;
    private readonly bool _required;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserIdInjectionFilter"/> class.
    /// </summary>
    /// <param name="claimType">
    /// The claim type to look for on the current user (defaults to <c>"UserId"</c>).
    /// </param>
    /// <param name="required">
    /// Indicates whether the claim is required. If <c>true</c> and no valid claim is found,
    /// the request is short-circuited with a <see cref="ForbidResult"/>.
    /// </param>
    public UserIdInjectionFilter(string claimType = "UserId", bool required = true)
    {
        _claimType = claimType;
        _required = required;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var principal = context.HttpContext.User;

        Guid? userId = null;
        var claimValue = principal?.FindFirst(_claimType)?.Value;
        if (!string.IsNullOrEmpty(claimValue) && Guid.TryParse(claimValue, out var parsed))
        {
            userId = parsed;
        }

        if (_required && userId is null)
        {
            context.Result = new ForbidResult();
            return;
        }

        if (userId is not null)
        {
            foreach (var (_, arg) in context.ActionArguments.ToArray())
            {
                if (arg is null) continue;

                switch (arg)
                {
                    case BaseRequest br:
                        if (br.UserId == Guid.Empty) br.UserId = userId.Value;
                        break;

                    default: break;
                }
            }
        }

        await next();
    }
}
