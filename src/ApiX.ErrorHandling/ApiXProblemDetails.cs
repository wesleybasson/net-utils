using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiX.ErrorHandling;

/// <summary>
/// An RFC7807 <see cref="ProblemDetails"/> type extended with ApiX-specific metadata.  
/// Used as the canonical error response payload for ApiX applications.
/// </summary>
public sealed class ApiXProblemDetails : ProblemDetails
{
    /// <summary>
    /// A stable, machine-readable error code (e.g. <c>"entity.not_found"</c>).
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// A trace or correlation identifier associated with the request,  
    /// typically <see cref="System.Diagnostics.Activity.Id"/> or <see cref="HttpContext.TraceIdentifier"/>.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// An optional payload providing additional error context (e.g. parameters or state).  
    /// Typically only included in Development responses for debugging.
    /// </summary>
    public object? Payload { get; set; }
}
