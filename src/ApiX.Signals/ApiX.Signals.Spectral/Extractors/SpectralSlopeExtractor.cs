using ApiX.Signals.Core.Abstractions;
using ApiX.Signals.Core.Features;
using ApiX.Signals.Spectral.Spectral;

namespace ApiX.Signals.Spectral.Extractors;

/// <summary>
/// Computes the spectral slope β (beta) of a signal’s power spectral density (PSD)
/// using Welch’s method. Implements <see cref="IPsdProvider"/> so that the PSD
/// can be reused by other extractors (e.g., <c>SpectralShapeExtractor</c>),
/// avoiding redundant FFTs.
/// </summary>
/// <remarks>
/// <para>
/// Interpretation of β:
/// <list type="bullet">
///   <item><description>β ≈ 0 → white noise (flat spectrum)</description></item>
///   <item><description>β &gt; 0 → persistence / long-memory processes</description></item>
///   <item><description>β &lt; 0 → anti-persistence or high-frequency dominance</description></item>
/// </list>
/// </para>
/// For fractional Gaussian noise (fGn), <c>H ≈ (β + 1) / 2</c>.
/// For fractional Brownian motion (fBm), <c>H ≈ (β − 1) / 2</c>.
/// </remarks>
public sealed class SpectralSlopeExtractor : IFeatureExtractor, IPsdProvider
{
    private readonly double _fs;
    private readonly int _segLen;
    private readonly int _overlap;

    private double[]? _lastFreq;
    private double[]? _lastPsd;

    /// <summary>
    /// Create a new <see cref="SpectralSlopeExtractor"/>.
    /// </summary>
    /// <param name="sampleHz">Sampling frequency in Hz (e.g. 1/30 for a 30s cadence).</param>
    /// <param name="segLen">Segment length for Welch PSD (default 128). Power of 2 is recommended.</param>
    /// <param name="overlap">Overlap between Welch segments (default 64).</param>
    public SpectralSlopeExtractor(double sampleHz, int segLen = 128, int overlap = 64)
    {
        _fs = sampleHz;
        _segLen = segLen;
        _overlap = overlap;
    }

    /// <inheritdoc/>
    public string Name => "beta";

    /// <inheritdoc/>
    public void Extract(ReadOnlySpan<double> window, ref FeatureVector dst)
    {
        var (f, psd) = Features.WelchPsd(window, _fs, segLen: _segLen, overlap: _overlap);
        if (f.Length == 0) return;

        // Cache for IPsdProvider
        _lastFreq = f;
        _lastPsd = psd;

        // Estimate spectral slope β on low frequencies
        var (beta, r2) = Features.SpectralSlopeBeta(f, psd, fMaxRatio: 0.1);

        dst.Beta = beta;
        dst.FitR2 = Math.Min(dst.FitR2, r2);
    }

    /// <summary>
    /// Returns the most recently computed PSD (frequency &amp; power arrays)
    /// for the same window, allowing other extractors to reuse it.
    /// </summary>
    public (double[] freq, double[] psd)? TryGet(ReadOnlySpan<double> window)
    {
        if (_lastFreq is null || _lastPsd is null)
            return null;

        return (_lastFreq, _lastPsd);
    }
}
