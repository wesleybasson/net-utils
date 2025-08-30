using ApiX.Signals.Core.Features;

namespace ApiX.Signals.Core.Abstractions;

/// <summary>
/// 
/// </summary>
public interface ISignalPipeline
{
    /// <summary>
    /// Add/replace transforms and feature extractors.
    /// </summary>
    ISignalPipeline Use(ITransformer transformer);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="extractor"></param>
    /// <returns></returns>
    ISignalPipeline Use(IFeatureExtractor extractor);

    /// <summary>
    /// Push a single sample; returns current features if window is ready.
    /// </summary>
    bool TryPush(double value, DateTimeOffset ts, out FeatureVector vector);
}
