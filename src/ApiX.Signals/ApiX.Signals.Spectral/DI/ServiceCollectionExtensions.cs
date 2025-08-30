using ApiX.Signals.Core.Abstractions;
using ApiX.Signals.Spectral.Extractors;
using Microsoft.Extensions.DependencyInjection;

namespace ApiX.Signals.Spectral.DI;

/// <summary>
/// Service registration helpers for spectral feature extractors.
/// Ensures a single shared <see cref="SpectralSlopeExtractor"/> instance
/// is used as both <see cref="IFeatureExtractor"/> and <see cref="IPsdProvider"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers spectral extractors: slope (β) and shape (entropy/flatness/peak).
    /// The slope extractor is shared and exposed as <see cref="IPsdProvider"/> so
    /// the shape extractor can reuse its PSD.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="sampleHz">Sampling frequency in Hz (e.g., 1/30).</param>
    /// <param name="segLen">Welch segment length (default 128).</param>
    /// <param name="overlap">Welch overlap (default 64).</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddSpectralExtractors(
        this IServiceCollection services,
        double sampleHz,
        int segLen = 128,
        int overlap = 64)
    {
        // Register ONE shared slope extractor instance
        services.AddSingleton<SpectralSlopeExtractor>(_ =>
            new SpectralSlopeExtractor(sampleHz, segLen, overlap));

        // Expose it as both IFeatureExtractor and IPsdProvider
        services.AddSingleton<IFeatureExtractor>(sp =>
            sp.GetRequiredService<SpectralSlopeExtractor>());

        services.AddSingleton<IPsdProvider>(sp =>
            sp.GetRequiredService<SpectralSlopeExtractor>());

        // Register the shape extractor; DI will inject IPsdProvider automatically
        services.AddSingleton<IFeatureExtractor>(sp =>
            new SpectralShapeExtractor(
                sampleHz: sampleHz,
                segLen: segLen,
                overlap: overlap,
                psdProvider: sp.GetRequiredService<IPsdProvider>()));

        return services;
    }
}
