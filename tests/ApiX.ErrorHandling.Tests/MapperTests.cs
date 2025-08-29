using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApiX.ErrorHandling.Tests;

public static class TestHelpers
{
    public static (DefaultExceptionMapper mapper, ProblemDetailsFactory factory, ApiXErrorOptions options) CreateMapper(bool includeDetailsInDev = true)
    {
        var services = new ServiceCollection();
        services.AddMvcCore();
        services.AddLogging();
        services.AddProblemDetails(); // registers ProblemDetailsFactory
        var sp = services.BuildServiceProvider();

        var options = new ApiXErrorOptions { IncludeDetailsInDevelopment = includeDetailsInDev };
        var mapper = new DefaultExceptionMapper(Options.Create(options), new NullLogger<DefaultExceptionMapper>());
        var factory = sp.GetRequiredService<ProblemDetailsFactory>();
        return (mapper, factory, options);
    }

    public static HttpContext NewHttpContext() => new DefaultHttpContext();
}

public class MapperTests
{
    [Fact]
    public void Maps_ApiXException_With_Code_Status_And_Message()
    {
        var (mapper, factory, _) = TestHelpers.CreateMapper();
        var ctx = TestHelpers.NewHttpContext();

        var ex = new ApiXException("entity.not_found", StatusCodes.Status404NotFound, "Nope", new { id = 7 });
        var pd = mapper.Map(ctx, ex, includeDetails: true, factory);

        Assert.Equal(404, pd.Status);
        Assert.Equal("Nope", pd.Title);
        Assert.Equal("entity.not_found", pd.Code);
        Assert.NotNull(pd.TraceId);
        Assert.NotNull(pd.Payload);
    }

    [Fact]
    public void Maps_ArgumentException_To_400()
    {
        var (mapper, factory, _) = TestHelpers.CreateMapper();
        var ctx = TestHelpers.NewHttpContext();

        var ex = new ArgumentException("bad arg");
        var pd = mapper.Map(ctx, ex, includeDetails: false, factory);

        Assert.Equal(400, pd.Status);
        Assert.Equal("An error occurred.", pd.Title); // DefaultTitle when includeDetails false
        Assert.Null(pd.Payload);
    }

    [Fact]
    public void Includes_Details_Only_When_Enabled()
    {
        var (mapper, factory, _) = TestHelpers.CreateMapper(includeDetailsInDev: true);
        var ctx = TestHelpers.NewHttpContext();
        var ex = new InvalidOperationException("boom");

        var pdDev = mapper.Map(ctx, ex, includeDetails: true, factory);
        Assert.Contains("boom", pdDev.Title);
        Assert.NotNull(pdDev.Detail);

        var pdProd = mapper.Map(ctx, ex, includeDetails: false, factory);
        Assert.Equal("An error occurred.", pdProd.Title);
        Assert.Null(pdProd.Detail);
    }
}
