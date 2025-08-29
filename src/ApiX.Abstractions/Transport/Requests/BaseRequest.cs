namespace ApiX.Abstractions.Transport.Requests;

/// <summary>
/// Represents the base type for all request models in the ApiX libraries,
/// enforcing the inclusion of <see cref="ApiKey"/> and <see cref="UserId"/> 
/// for validation and authorization purposes.
/// </summary>
public abstract class BaseRequest : IApiKeyAndUserRequest
{
    /// <summary>
    /// Gets or sets the identifier of the user making the request.
    /// Defaults to <see cref="Guid.Empty"/> if not provided.
    /// </summary>
    public virtual Guid UserId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the API key associated with the request.
    /// Used to authenticate the caller or client application.
    /// Defaults to <see cref="string.Empty"/>.
    /// </summary>
    public virtual string ApiKey { get; set; } = string.Empty;
}
