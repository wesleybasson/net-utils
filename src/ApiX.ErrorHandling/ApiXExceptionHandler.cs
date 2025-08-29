using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ApiX.ErrorHandling;

/// <summary>
/// Configures and installs the ApiX global exception handling pipeline.  
/// Provides <see cref="AddApiXErrorHandling"/> for DI setup and <see cref="Handler"/>  
/// for use with <c>app.UseExceptionHandler(...)</c>.
/// </summary>
public static class ApiXExceptionHandler
{
    /// <summary>
    /// Registers ApiX error handling services:
    /// <list type="bullet">
    ///   <item><description>Adds the built-in <see cref="ProblemDetailsFactory"/> via <c>AddProblemDetails()</c>.</description></item>
    ///   <item><description>Binds <see cref="ApiXErrorOptions"/> for configuration.</description></item>
    ///   <item><description>Registers <see cref="DefaultExceptionMapper"/> as the default <see cref="IExceptionToProblemMapper"/>.</description></item>
    /// </list>
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configure">
    /// An optional delegate to configure <see cref="ApiXErrorOptions"/>  
    /// (e.g. adding custom status mappings or toggling detail inclusion).
    /// </param>
    public static void AddApiXErrorHandling(this IServiceCollection services, Action<ApiXErrorOptions>? configure = null)
    {
        services.AddProblemDetails(); // .NET built-in RFC7807 factory
        services.Configure(configure ?? (_ => { }));
        services.AddSingleton<IExceptionToProblemMapper, DefaultExceptionMapper>();
    }

    /// <summary>
    /// Exception handler endpoint for use with <c>app.UseExceptionHandler(ApiXExceptionHandler.Handler)</c>.  
    /// Translates unhandled exceptions into consistent <see cref="ApiXProblemDetails"/> responses.
    /// </summary>
    /// <param name="app">The application builder pipeline.</param>
    public static void Handler(IApplicationBuilder app)
    {
        app.Run(async ctx =>
        {
            var env = ctx.RequestServices.GetRequiredService<IHostEnvironment>();
            var opts = ctx.RequestServices.GetRequiredService<IOptions<ApiXErrorOptions>>().Value;
            var includeDetails = env.IsDevelopment() && opts.IncludeDetailsInDevelopment;

            var feature = ctx.Features.Get<IExceptionHandlerFeature>();
            var ex = feature?.Error;

            // No exception? Return 500 for consistency
            if (ex is null)
            {
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await ctx.Response.WriteAsJsonAsync(new ApiXProblemDetails
                {
                    Status = 500,
                    Title = opts.DefaultTitle,
                    TraceId = Activity.Current?.Id ?? ctx.TraceIdentifier,
                    Code = ErrorCode.Generic_Error.ToStringCode()
                });
                return;
            }

            var factory = ctx.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var mapper = ctx.RequestServices.GetRequiredService<IExceptionToProblemMapper>();
            var problem = mapper.Map(ctx, ex, includeDetails, factory);

            ctx.Response.ContentType = "application/problem+json";
            ctx.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

            await ctx.Response.WriteAsJsonAsync(problem, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        });
    }
}
