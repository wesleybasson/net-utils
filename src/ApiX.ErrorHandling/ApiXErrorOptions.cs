using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiX.ErrorHandling;

/// <summary>
/// Options controlling how ApiX error responses are generated.
/// Configure via <c>builder.Services.AddApiXErrorHandling(options =&gt; ...)</c>.
/// </summary>
public sealed class ApiXErrorOptions
{
    /// <summary>
    /// When <c>true</c>, stack traces, exception messages, and payloads are included in
    /// responses if the host environment is <c>Development</c>.  
    /// In production, these details are hidden regardless of this setting.
    /// </summary>
    public bool IncludeDetailsInDevelopment { get; set; } = true;

    /// <summary>
    /// The default <see cref="ProblemDetails.Title"/> value used when exception
    /// details are hidden (typically in Production).
    /// </summary>
    public string DefaultTitle { get; set; } = "An error occurred.";

    /// <summary>
    /// A mapping of common exception types to their default HTTP status codes.  
    /// This mapping is consulted when handling exceptions that are not of type
    /// <see cref="ApiXException"/>.
    /// </summary>
    public Dictionary<Type, int> StatusMap { get; } = new()
    {
        { typeof(ArgumentException),           StatusCodes.Status400BadRequest },
        { typeof(UnauthorizedAccessException), StatusCodes.Status401Unauthorized },
        { typeof(KeyNotFoundException),        StatusCodes.Status404NotFound },
        { typeof(NotImplementedException),     StatusCodes.Status501NotImplemented },
        { typeof(TimeoutException),            StatusCodes.Status504GatewayTimeout },
        { typeof(HttpRequestException),        StatusCodes.Status502BadGateway },
        { typeof(ObjectDisposedException),     StatusCodes.Status410Gone },
        { typeof(OperationCanceledException),  499 } // Client Closed Request (nginx convention)
    };
}
