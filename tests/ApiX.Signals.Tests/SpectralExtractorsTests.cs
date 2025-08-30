using ApiX.Signals.Spectral.Extractors;
using ApiX.Signals.Spectral.Spectral;

namespace ApiX.Signals.Tests;

public class SpectralExtractorsTests
{
    private const double Fs = 1.0 / 30.0; // 30s cadence ~ 0.0333 Hz

    [Fact]
    public void SpectralSlope_WhiteNoise_BetaNearZero()
    {
        var x = SignalGenerators.WhiteNoise(512);
        var (f, psd) = Features.WelchPsd(x, Fs, segLen: 128, overlap: 64);
        var (beta, r2) = Features.SpectralSlopeBeta(f, psd, fMaxRatio: 0.1);

        Assert.InRange(beta, -0.3, 0.3);
        Assert.InRange(r2, 0.20, 1.01);
    }

    [Fact]
    public void SpectralShape_Sine_DetectsPeakAndLowFlatness()
    {
        double targetHz = 1.0 / 600.0; // 10-minute cycle
        var x = SignalGenerators.SinePlusNoise(2048, targetHz, Fs, snr: 2.5);
        var (f, psd) = Features.WelchPsd(x, Fs, segLen: 256, overlap: 128);
        var (entropy, flat, topPeakHz, peakRatio) = Features.ShapeFeatures(f, psd);

        Assert.True(peakRatio > 0.10);     // non-trivial energy at peak
        Assert.True(flat < 0.6);           // tonal-ish -> lower flatness
        Assert.True(entropy < 0.9);        // some order present
        Assert.InRange(topPeakHz, targetHz * 0.5, targetHz * 1.5); // rough neighborhood
    }

    [Fact]
    public void SpectralShape_ReusesPsdFromProvider()
    {
        // Arrange: compute a PSD once, stuff it into a stub provider,
        // and ensure SpectralShape uses that PSD and matches direct computation.
        var x = SignalGenerators.WhiteNoise(1024);
        var (freq, psd) = Features.WelchPsd(x, Fs, segLen: 256, overlap: 128);
        var (Entropy, Flatness, TopPeakHz, PeakPowerRatio) = Features.ShapeFeatures(freq, psd);

        var provider = new StubPsdProvider(freq, psd);
        var shape = new SpectralShapeExtractor(sampleHz: Fs, segLen: 256, overlap: 128, psdProvider: provider);

        Core.Features.FeatureVector dst = default;
        shape.Extract(x, ref dst);

        Assert.InRange(dst.SpecEntropy, Entropy - 1e-9, Entropy + 1e-9);
        Assert.InRange(dst.SpecFlatness, Flatness - 1e-9, Flatness + 1e-9);
        Assert.InRange(dst.TopPeakHz, TopPeakHz - 1e-9, TopPeakHz + 1e-9);
        Assert.InRange(dst.PeakPowerRatio, PeakPowerRatio - 1e-9, PeakPowerRatio + 1e-9);
    }

    private sealed class StubPsdProvider : IPsdProvider
    {
        private readonly (double[] f, double[] p) _data;
        public StubPsdProvider(double[] f, double[] p) => _data = (f, p);
        public (double[] freq, double[] psd)? TryGet(ReadOnlySpan<double> _) => _data;
    }
}
