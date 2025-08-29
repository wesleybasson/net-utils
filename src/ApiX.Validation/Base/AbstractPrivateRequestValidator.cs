using ApiX.Abstractions.Transport.Requests;
using FluentValidation;

namespace ApiX.Validation.Base;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class AbstractPrivateRequestValidator<T>
    : AbstractPublicRequestValidator<T>
    where T : IApiKeyAndUserRequest
{
    /// <summary>
    /// 
    /// </summary>
    protected AbstractPrivateRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithMessage("User Id must be supplied");
    }
}
