using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;

namespace ApiX.AspNetCore.Middleware;

/// <summary>
/// 
/// </summary>
public static class ErrorHandler
{
    private static Func<Exception, int>? _customExceptionExtension;
    private static bool _includePayload;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="environment"></param>
    /// <param name="customExceptionExtension"></param>
    /// <param name="includePayload"></param>
    public static void UseCustomErrors(this IApplicationBuilder app, IHostEnvironment environment,
        Func<Exception, int>? customExceptionExtension, bool includePayload = true)
    {
        _customExceptionExtension = customExceptionExtension;
        _includePayload = includePayload;
        if (environment.IsDevelopment())
        {
            app.Use(WriteDevelopmentResponse);
        }
        else
        {
            app.Use(WriteProductionResponse);
        }
    }

    private static Task WriteDevelopmentResponse(HttpContext httpContext, Func<Task> next)
        => WriteResponse(httpContext, includeDetails: true);

    private static Task WriteProductionResponse(HttpContext httpContext, Func<Task> next)
        => WriteResponse(httpContext, includeDetails: false);

    private static async Task WriteResponse(HttpContext httpContext, bool includeDetails)
    {
        var exceptionDetails = httpContext.Features.Get<IExceptionHandlerFeature>();
        var ex = exceptionDetails?.Error;

        if (ex != null)
        {
            //httpContext.Response.ContentType = "application/problem+json";
            //var title = includeDetails ? "An error occurred: " + ex.Message : "An error occurred!";
            //var details = includeDetails ? ex.ToString() : null;

            //var statusCode = _customExceptionExtension?.Invoke(ex) ?? 0;
            //if (statusCode == 0) statusCode = GetStatusCode(ex);
            //var type = GetErrorType(ex);

            //httpContext.Response.StatusCode = statusCode;

            //var problem = new ProblemDetails
            //{
            //    Title = title,
            //    Type = type,
            //    Detail = details,
            //    Status = statusCode,
            //};

            //var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            //problem.Extensions["traceId"] = traceId;

            //var payloadProperty = ex.GetType().GetProperty("Payload");
            //if (payloadProperty != null)
            //{
            //    var payload = payloadProperty.GetValue(ex);
            //    if (payload != null)
            //    {
            //        if (_includePayload) problem.Extensions["Payload"] = payload;
            //        foreach (var property in payload.GetType().GetProperties())
            //        {
            //            var key = property.Name;
            //            problem.Extensions[key] = property.GetValue(payload, null);
            //        }
            //    }
            //}

            //httpContext.Response.ContentType = "application/json";
            //var currentContent = httpContext.Response.Body;
            //await JsonSerializer.SerializeAsync(currentContent, problem,
            //    new JsonSerializerOptions
            //    {
            //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            //    });
            await Task.CompletedTask;
        }
    }

    private static int GetStatusCode(Exception ex) =>
        ex switch
        {
            // Custom Exceptions:
            //InvalidRequestException => 400,
            //UnauthorizedException => 401,
            //ForbiddenException => 403,
            //ItemNotFoundException => 404,
            //DuplicateRecordException => 409,
            //LegalMatterException => 451,
            //ServiceNotAvailableException => 503,
            // Core Exceptions:
            ArgumentException => 400,
            HttpRequestException => 502,
            TimeoutException => 504,
            ObjectDisposedException => 410,
            NotImplementedException => 501,
            _ => 500
        };

    private static string GetErrorType(Exception ex) =>
        ex switch
        {
            //Custom Exceptions:
            //InvalidRequestException => "Bad Request",
            //UnauthorizedException => "Unauthorized",
            //ForbiddenException => "Forbidden",
            //ItemNotFoundException => "Not Found",
            //DuplicateRecordException => "Conflict",
            //LegalMatterException => "Unavailable For Legal Reasons",
            //ServiceNotAvailableException => "Service Unavailable",
            // Core Exceptions:
            ArgumentException => "Bad Request",
            HttpRequestException => "Bad Gateway",
            TimeoutException => "Gateway Timeout",
            ObjectDisposedException => "Gone",
            NotImplementedException => "Not Implemented",
            _ => "Internal Server Error"
        };
}

