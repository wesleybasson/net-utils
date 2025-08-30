using ApiX.Signals.Core.Abstractions;
using ApiX.Signals.Core.Pipelines;
using ApiX.Signals.Core.Transforms;
using ApiX.Signals.Fractal.Extractors;
using ApiX.Signals.Spectral.DI;
using Microsoft.Extensions.DependencyInjection;

namespace ApiX.Signals.Tests;

public class PipelineEndToEndTests
{
    [Fact]
    public void Pipeline_Composes_Transforms_And_Extractors()
    {
        // DI: register spectral slope+shape and a couple of fractal extractors
        var services = new ServiceCollection();
        services.AddSpectralExtractors(sampleHz: 1.0 / 30.0, segLen: 128, overlap: 64);
        services.AddSingleton<IFeatureExtractor>(new HurstDfaExtractor());
        services.AddSingleton<IFeatureExtractor>(new HiguchiExtractor(8));

        // Build a pipeline using DI-provided extractors
        services.AddSingleton<ISignalPipeline>(sp =>
        {
            var pipe = new SignalPipeline(windowSize: 256);
            pipe.Use(new FirstDifference()); // make stationary
            foreach (var ext in sp.GetServices<IFeatureExtractor>())
                pipe.Use(ext);
            return pipe;
        });

        var sp = services.BuildServiceProvider();
        var pipeline = sp.GetRequiredService<ISignalPipeline>();

        // Push samples
        var x = SignalGenerators.SinePlusNoise(512, freqHz: 1.0 / 900.0, sampleHz: 1.0 / 30.0, snr: 2.0);
        ApiX.Signals.Core.Features.FeatureVector vec = default;
        bool got = false;
        for (int i = 0; i < x.Length; i++)
            got = pipeline.TryPush(x[i], DateTimeOffset.UtcNow, out vec);

        Assert.True(got, "Pipeline did not produce a feature vector.");
        Assert.True((vec.Flags & 0x1) != 0); // Ready flag

        // Basic sanity: fields are populated with finite values
        Assert.True(double.IsFinite(vec.Hurst));
        Assert.True(double.IsFinite(vec.HiguchiFd));
        Assert.True(double.IsFinite(vec.Beta));
        Assert.True(double.IsFinite(vec.SpecEntropy));
        Assert.True(double.IsFinite(vec.SpecFlatness));
        Assert.True(double.IsFinite(vec.TopPeakHz));
        Assert.True(double.IsFinite(vec.PeakPowerRatio));
    }
}
