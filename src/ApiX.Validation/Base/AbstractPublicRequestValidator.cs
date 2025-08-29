using ApiX.Abstractions.Transport.Requests;
using FluentValidation;

namespace ApiX.Validation.Base;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class AbstractPublicRequestValidator<T> :
    AbstractValidator<T>
    where T : IApiKeyRequest
{
    /// <summary>
    /// 
    /// </summary>
    protected AbstractPublicRequestValidator()
    {
        RuleFor(x => x.ApiKey)
            .NotEmpty()
            .WithMessage("A valid API Key must be provided");
    }
}
