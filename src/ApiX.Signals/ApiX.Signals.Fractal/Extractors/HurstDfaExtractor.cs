using ApiX.Signals.Core.Abstractions;
using ApiX.Signals.Core.Features;
using ApiX.Signals.Fractal.Dfa;

namespace ApiX.Signals.Fractal.Extractors;

/// <summary>
/// 
/// </summary>
public sealed class HurstDfaExtractor : IFeatureExtractor
{
    /// <inheritdoc/>
    public string Name => "hurst_dfa";

    /// <inheritdoc/>
    public void Extract(ReadOnlySpan<double> window, ref FeatureVector dst)
    {
        var (H, r2, _) = HurstDfa.Compute(window);
        dst.Hurst = H;
        // Keep the better (min) fit confidence between DFA/line for diagnostics
        dst.FitR2 = Math.Min(dst.FitR2, r2);
    }
}
