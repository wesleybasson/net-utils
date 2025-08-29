using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace ApiX.ErrorHandling;

/// <summary>
/// Extension methods for producing consistent success and error responses
/// using ASP.NET Core Minimal API <see cref="IResult"/> primitives.
/// </summary>
public static class ResultsExtensions
{
    /// <summary>
    /// Produces a standardized RFC7807-style <c>ProblemDetails</c> JSON response
    /// with an ApiX error <see cref="ErrorCode"/>, status code, and optional payload.
    /// </summary>
    /// <param name="_">Extension target (not used).</param>
    /// <param name="code">The machine-readable <see cref="ErrorCode"/> representing the error.</param>
    /// <param name="status">The HTTP status code to return.</param>
    /// <param name="message">
    /// An optional human-readable message. If not supplied, the default message
    /// associated with the error code is used.
    /// </param>
    /// <param name="payload">
    /// Optional contextual object with extra error information. Will be included
    /// in the serialized response only when configured.
    /// </param>
    /// <param name="templateParameters">
    /// Optional parameters to substitute into the error message if the message
    /// contains placeholders (e.g. <c>{Id}</c>).
    /// </param>
    /// <returns>
    /// An <see cref="IResult"/> representing a JSON response with the supplied
    /// status code and a structured <see cref="ApiXProblemDetails"/> payload.
    /// </returns>
    public static IResult Problem(
        this IResultExtensions _,
        ErrorCode code,
        int status,
        string? message = null,
        object? payload = null,
        object? templateParameters = null)
    {
        var msg = ErrorCatalog.Format(message ?? code.DefaultMessage(), templateParameters);
        var pd = new ApiXProblemDetails
        {
            Status = status,
            Title = msg,
            Code = code.ToStringCode(),
            TraceId = Activity.Current?.Id // TraceId can be enriched further by the handler
        };
        return Results.Json(pd, statusCode: status);
    }

    /// <summary>
    /// Produces a standardized "success" response wrapping a payload object.
    /// </summary>
    /// <typeparam name="T">The type of the payload object.</typeparam>
    /// <param name="payload">The value to return to the client.</param>
    /// <param name="message">
    /// An optional message. Defaults to <c>"Success"</c>.
    /// </param>
    /// <returns>
    /// An <see cref="IResult"/> representing a JSON response with HTTP 200 OK,
    /// containing a success code, message, and the supplied payload.
    /// </returns>
    public static IResult Ok<T>(T payload, string? message = null) =>
        Results.Ok(new
        {
            code = "success",
            message = message ?? "Success",
            payload
        });
}
