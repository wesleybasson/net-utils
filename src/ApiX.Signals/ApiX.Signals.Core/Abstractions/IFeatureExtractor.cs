using ApiX.Signals.Core.Features;

namespace ApiX.Signals.Core.Abstractions;

/// <summary>
/// 
/// </summary>
public interface IFeatureExtractor
{
    /// <summary>
    /// Compute features over the given series window.
    /// </summary>
    void Extract(ReadOnlySpan<double> window, ref FeatureVector dst);

    /// <summary>
    /// 
    /// </summary>
    string Name { get; }
}
