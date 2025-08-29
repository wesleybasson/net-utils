namespace ApiX.Abstractions.Transport.Requests;

/// <summary>
/// Defines a contract for request models that require
/// an API key for authentication or identification.
/// </summary>
public interface IApiKeyRequest
{
    /// <summary>
    /// Gets the API key associated with the request.
    /// </summary>
    string ApiKey { get; }
}
