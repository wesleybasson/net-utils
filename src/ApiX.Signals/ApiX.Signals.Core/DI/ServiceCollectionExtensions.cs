using ApiX.Signals.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace ApiX.Signals.Core.DI;

/// <summary>
/// 
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="windowSize"></param>
    /// <param name="factory"></param>
    /// <returns></returns>
    public static IServiceCollection AddFractalSignalPipeline(
        this IServiceCollection services,
        int windowSize,
        Func<IServiceProvider, ISignalPipeline> factory)
    {
        services.AddSingleton<ISignalPipeline>(sp => factory(sp));
        return services;
    }
}
