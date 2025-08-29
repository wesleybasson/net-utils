namespace ApiX.Abstractions.Transport.Requests;

/// <summary>
/// Defines a contract for request models that require both 
/// an API key and a user identifier.
/// </summary>
public interface IApiKeyAndUserRequest : IApiKeyRequest
{
    /// <summary>
    /// Gets the identifier of the user associated with the request.
    /// </summary>
    Guid UserId { get; }
}
