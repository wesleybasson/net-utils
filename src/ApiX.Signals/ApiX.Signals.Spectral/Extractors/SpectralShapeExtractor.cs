using ApiX.Signals.Core.Abstractions;
using ApiX.Signals.Core.Features;
using ApiX.Signals.Spectral.Extractors;
using ApiX.Signals.Spectral.Spectral;

/// <summary>
/// Computes spectral "shape" descriptors from a power spectral density (PSD):
/// normalized spectral entropy, spectral flatness (geo/arith), strongest peak
/// frequency, and the top-peak power ratio. If an <see cref="IPsdProvider"/> is
/// present in DI, it will reuse the cached PSD produced by another extractor
/// (e.g., <see cref="SpectralSlopeExtractor"/>) to avoid a second FFT.
/// </summary>
/// <remarks>
/// Intended to be domain-agnostic: works for any stationary-ish windowed series
/// (after your chosen pre-transforms). The outputs populate
/// <see cref="FeatureVector.SpecEntropy"/>, <see cref="FeatureVector.SpecFlatness"/>,
/// <see cref="FeatureVector.TopPeakHz"/>, and <see cref="FeatureVector.PeakPowerRatio"/>.
/// </remarks>
public sealed class SpectralShapeExtractor : IFeatureExtractor
{
    private readonly double _sampleHz;
    private readonly int _segLen;
    private readonly int _overlap;
    private readonly IPsdProvider? _psdProvider;

    /// <summary>
    /// Create a new <see cref="SpectralShapeExtractor"/>.
    /// </summary>
    /// <param name="sampleHz">Sampling frequency in Hz (e.g., 1/30 for 30-second cadence).</param>
    /// <param name="segLen">Welch segment length (power of two recommended; default 128).</param>
    /// <param name="overlap">Overlap between Welch segments (default 64).</param>
    /// <param name="psdProvider">
    /// Optional PSD provider. When supplied (e.g., via DI), the extractor will reuse
    /// the already-computed PSD instead of recomputing it.
    /// </param>
    public SpectralShapeExtractor(
        double sampleHz,
        int segLen = 128,
        int overlap = 64,
        IPsdProvider? psdProvider = null)
    {
        _sampleHz = sampleHz;
        _segLen = segLen;
        _overlap = overlap;
        _psdProvider = psdProvider;
    }

    /// <inheritdoc/>
    public string Name => "spectral_shape";

    /// <inheritdoc/>
    public void Extract(ReadOnlySpan<double> window, ref FeatureVector dst)
    {
        // Prefer a cached PSD if a provider is available
        (double[] freq, double[] psd)? cached = _psdProvider?.TryGet(window);
        double[] f, Pxx;

        if (cached is { } ok)
        {
            f = ok.freq; Pxx = ok.psd;
        }
        else
        {
            var (freq, psd) = Features.WelchPsd(window, _sampleHz, _segLen, _overlap);
            if (freq.Length == 0) return;
            f = freq; Pxx = psd;
        }

        var (entropy, flatness, topPeakHz, peakRatio) = Features.ShapeFeatures(f, Pxx);

        dst.SpecEntropy = entropy;   // [0,1] — 1 ~ very noise-like
        dst.SpecFlatness = flatness;  // ~1 white; ~0 tonal/peaky
        dst.TopPeakHz = topPeakHz; // Hz of strongest peak
        dst.PeakPowerRatio = peakRatio; // fraction of total power near the top peak
    }
}