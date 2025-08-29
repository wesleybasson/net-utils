using ApiX.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ApiX.AspNetCore.Attributes;

/// <summary>
/// Attribute for applying <see cref="UserIdInjectionFilter"/> to controllers or actions.
/// </summary>
/// <remarks>
/// <para>
/// This attribute injects the current user's <c>UserId</c> claim into action arguments
/// of type <see cref="Abstractions.Transport.Requests.BaseRequest"/>.
/// </para>
/// <para>
/// It can be applied at the controller or action level, and accepts parameters to configure
/// the claim type and whether the claim is required.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class UserIdInjectionAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserIdInjectionAttribute"/> class.
    /// </summary>
    /// <param name="claimType">
    /// The claim type to look for on the current user (defaults to <c>"UserId"</c>).
    /// </param>
    /// <param name="required">
    /// Indicates whether the claim is required. If <c>true</c> and no valid claim is found,
    /// the request is short-circuited with a <see cref="ForbidResult"/>.
    /// </param>
    public UserIdInjectionAttribute(string claimType = "UserId", bool required = true)
        : base(typeof(UserIdInjectionFilter))
    {
        Arguments = [claimType, required];
        Order = int.MinValue + 100;
    }
}
