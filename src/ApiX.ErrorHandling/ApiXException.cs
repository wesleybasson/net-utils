namespace ApiX.ErrorHandling;

/// <summary>
/// A single, general-purpose exception type for ApiX applications.  
/// Encapsulates a stable machine-readable error <see cref="Code"/>, an HTTP <see cref="Status"/>,
/// and optional context such as a <see cref="Payload"/> or arbitrary <see cref="Extensions"/>.
/// </summary>
public class ApiXException : Exception
{
    /// <summary>
    /// A stable machine-readable identifier for the error, e.g. <c>"auth.user_exists"</c>.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// The HTTP status code that best represents this error (e.g. 400, 404, 409).
    /// </summary>
    public int Status { get; }

    /// <summary>
    /// An optional object containing contextual information about the error.  
    /// Typically included only in Development responses.
    /// </summary>
    public object? Payload { get; }

    /// <summary>
    /// An arbitrary extension dictionary for additional metadata about the error.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Extensions { get; }

    /// <summary>
    /// Creates a new <see cref="ApiXException"/>.
    /// </summary>
    /// <param name="code">The stable machine-readable error code (e.g. <c>"entity.not_found"</c>).</param>
    /// <param name="status">The HTTP status code to return (e.g. 404, 409).</param>
    /// <param name="message">Optional human-readable message. Defaults to <paramref name="code"/>.</param>
    /// <param name="payload">Optional contextual payload object for additional details.</param>
    /// <param name="extensions">Optional additional metadata extensions.</param>
    /// <param name="inner">Optional inner exception that caused this error.</param>
    public ApiXException(
        string code,
        int status,
        string? message = null,
        object? payload = null,
        IReadOnlyDictionary<string, object?>? extensions = null,
        Exception? inner = null)
        : base(message ?? code, inner)
    {
        Code = code;
        Status = status;
        Payload = payload;
        Extensions = extensions ?? new Dictionary<string, object?>();
    }
}
