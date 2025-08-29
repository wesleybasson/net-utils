using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiX.ErrorHandling;

/// <summary>
/// Maps exceptions to RFC7807 <see cref="ProblemDetails"/> objects for consistent error responses.  
/// Implementations can add app-specific conventions such as error code mapping or validation shaping.
/// </summary>
public interface IExceptionToProblemMapper
{
    /// <summary>
    /// Creates an <see cref="ApiXProblemDetails"/> instance for the given exception.
    /// </summary>
    /// <param name="http">The current <see cref="HttpContext"/>.</param>
    /// <param name="ex">The exception to map.</param>
    /// <param name="includeDetails">
    /// Whether to include detailed information such as exception messages,
    /// stack traces, and payloads (usually only in Development).
    /// </param>
    /// <param name="factory">
    /// The <see cref="ProblemDetailsFactory"/> used to create the base <see cref="ProblemDetails"/>.
    /// </param>
    /// <returns>An <see cref="ApiXProblemDetails"/> instance describing the exception.</returns>
    ApiXProblemDetails Map(HttpContext http, Exception ex, bool includeDetails, ProblemDetailsFactory factory);
}

/// <summary>
/// Default implementation of <see cref="IExceptionToProblemMapper"/>.  
/// <list type="bullet">
///   <item><description>Handles <see cref="ApiXException"/> specially, using its code, status, and payload.</description></item>
///   <item><description>Falls back to <see cref="ApiXErrorOptions.StatusMap"/> for well-known exception types.</description></item>
///   <item><description>Includes messages, stack traces, and payloads when enabled in Development.</description></item>
///   <item><description>Detects FluentValidation validation errors (via reflection) and attaches field-level errors.</description></item>
/// </list>
/// </summary>
public sealed class DefaultExceptionMapper : IExceptionToProblemMapper
{
    private readonly ApiXErrorOptions _options;
    private readonly ILogger<DefaultExceptionMapper> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExceptionMapper"/> class.
    /// </summary>
    /// <param name="options">Configuration options controlling error mapping behavior.</param>
    /// <param name="log">Logger used to record exception details.</param>
    public DefaultExceptionMapper(IOptions<ApiXErrorOptions> options, ILogger<DefaultExceptionMapper> log)
    {
        _options = options.Value;
        _log = log;
    }

    /// <inheritdoc />
    public ApiXProblemDetails Map(HttpContext http, Exception ex, bool includeDetails, ProblemDetailsFactory factory)
    {
        var traceId = Activity.Current?.Id ?? http.TraceIdentifier;

        int status;
        string title;
        string? detail = null;
        string? code = null;
        object? payload = null;

        if (ex is ApiXException apix)
        {
            status = apix.Status;
            code = apix.Code;
            title = includeDetails ? apix.Message : _options.DefaultTitle;
            detail = includeDetails ? ex.ToString() : null;
            payload = includeDetails ? apix.Payload : null;
        }
        else
        {
            if (!_options.StatusMap.TryGetValue(ex.GetType(), out status))
                status = StatusCodes.Status500InternalServerError;

            title = includeDetails ? ex.Message : _options.DefaultTitle;
            detail = includeDetails ? ex.ToString() : null;
        }

        // Log server-side details for diagnostics
        _log.LogError(ex, "Unhandled exception. TraceId={TraceId} Status={Status} Code={Code}",
            traceId, status, code ?? "n/a");

        // Use the ASP.NET Core factory to populate baseline ProblemDetails
        var pd = factory.CreateProblemDetails(http, statusCode: status, title: title, detail: detail);

        var result = new ApiXProblemDetails
        {
            Type = pd.Type,
            Title = pd.Title,
            Detail = pd.Detail,
            Status = pd.Status,
            Instance = pd.Instance,
            Code = code,
            TraceId = traceId,
            Payload = payload
        };

        foreach (var kvp in pd.Extensions)
            result.Extensions[kvp.Key] = kvp.Value;

        // Attempt to attach FluentValidation error details if available
        TryAttachFluentValidationErrors(ex, includeDetails, result);

        return result;
    }

    /// <summary>
    /// If the exception is a FluentValidation <c>ValidationException</c> and details are enabled,
    /// captures property-level validation errors into <c>ProblemDetails.Extensions["errors"]</c>.  
    /// Reflection is used to avoid a direct package dependency on FluentValidation.
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <param name="includeDetails">Whether detailed error information should be included.</param>
    /// <param name="dest">The <see cref="ApiXProblemDetails"/> to enrich.</param>
    private static void TryAttachFluentValidationErrors(Exception ex, bool includeDetails, ApiXProblemDetails dest)
    {
        if (!includeDetails) return;

        var fvExType = Type.GetType("FluentValidation.ValidationException, FluentValidation");
        if (fvExType is null || !fvExType.IsInstanceOfType(ex)) return;

        var errorsProp = fvExType.GetProperty("Errors");
        var errors = errorsProp?.GetValue(ex) as System.Collections.IEnumerable;
        if (errors is null) return;

        var enumerator = errors.GetEnumerator();
        if (!enumerator.MoveNext()) return;

        var failureType = enumerator.Current!.GetType();
        var propNameProp = failureType.GetProperty("PropertyName");
        var errorMsgProp = failureType.GetProperty("ErrorMessage");

        var dict = new Dictionary<string, List<string>>();

        foreach (var item in errors)
        {
            var prop = (string?)propNameProp?.GetValue(item) ?? string.Empty;
            var msg = (string?)errorMsgProp?.GetValue(item) ?? string.Empty;

            if (!dict.TryGetValue(prop, out var list))
                dict[prop] = list = new List<string>();
            list.Add(msg);
        }

        dest.Extensions["errors"] = dict.ToDictionary(k => k.Key, v => v.Value.ToArray());
        dest.Code ??= ErrorCode.Validation_Failed.ToStringCode();
    }
}
