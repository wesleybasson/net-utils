namespace ApiX.Abstractions.Transport.Responses;

/// <summary>
/// Represents the base type for all response models in the ApiX libraries,
/// providing standard metadata for request/response correlation.
/// </summary>
public abstract class BaseResponse
{
    /// <summary>
    /// Gets or sets the identifier of the originating request.
    /// Useful for tracing requests and responses across services.
    /// Defaults to <see cref="string.Empty"/>.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for this response instance.
    /// Automatically initialized with a new <see cref="Guid"/>.
    /// </summary>
    public string ResponseId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets an optional message providing context about the response,
    /// such as a status description, warning, or error detail.
    /// Defaults to <see cref="string.Empty"/>.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
