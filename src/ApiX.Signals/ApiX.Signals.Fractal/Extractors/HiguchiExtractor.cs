using ApiX.Signals.Core.Abstractions;
using ApiX.Signals.Core.Features;

namespace ApiX.Signals.Fractal.Extractors;

/// <summary>
/// 
/// </summary>
public sealed class HiguchiExtractor : IFeatureExtractor
{
    private readonly int _kMax;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="kMax"></param>
    public HiguchiExtractor(int kMax = 8) { _kMax = kMax; }

    /// <inheritdoc/>
    public string Name => "higuchi";

    /// <inheritdoc/>
    public void Extract(ReadOnlySpan<double> window, ref FeatureVector dst)
    {
        dst.HiguchiFd = Higuchi.Higuchi.FractalDimension(window, _kMax);
    }
}
