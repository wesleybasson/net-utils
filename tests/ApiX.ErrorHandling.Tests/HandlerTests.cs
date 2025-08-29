using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;

namespace ApiX.ErrorHandling.Tests;

public class HandlerTests
{
    private static IHost BuildHost(bool development)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                if (development) web.UseEnvironment("Development");

                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddMvcCore();
                    services.AddApiXErrorHandling(o => o.IncludeDetailsInDevelopment = true);
                });

                web.Configure(app =>
                {
                    app.UseExceptionHandler(ApiXExceptionHandler.Handler);

                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/apix", async context =>
                        {
                        await Task.FromException(new ApiXException("entity.not_found", 404, "Nope", new { id = 99 }));
                        });

                        endpoints.MapGet("/arg", async context =>
                        {
                            await Task.FromException(new ArgumentException("bad"));
                        });
                    });
                });
            });

        return builder.Start();
    }

    [Fact]
    public async Task Handler_Formats_ApiXException_As_Problem()
    {
        using var host = BuildHost(development: true);
        var client = host.GetTestClient();

        var resp = await client.GetAsync("/apix");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        var problem = await resp.Content.ReadFromJsonAsync<ApiXProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem!.Status);
        Assert.Equal("Nope", problem.Title);
        Assert.Equal("entity.not_found", problem.Code);
        Assert.NotNull(problem.TraceId);
        Assert.NotNull(problem.Payload);
    }

    [Fact]
    public async Task Handler_Maps_ArgumentException_To_400_In_Prod_Without_Details()
    {
        using var host = BuildHost(development: false);
        var client = host.GetTestClient();

        var resp = await client.GetAsync("/arg");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var problem = await resp.Content.ReadFromJsonAsync<ApiXProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("An error occurred.", problem.Title); // Hidden in prod
        Assert.Null(problem.Payload);
        Assert.NotNull(problem.TraceId);
    }
}
