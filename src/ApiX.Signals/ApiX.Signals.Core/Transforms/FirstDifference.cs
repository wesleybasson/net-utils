using ApiX.Signals.Core.Abstractions;

namespace ApiX.Signals.Core.Transforms;

/// <summary>
/// 
/// </summary>
public sealed class FirstDifference : ITransformer
{
    /// <inheritdoc/>
    public string Name => "diff";

    /// <inheritdoc/>
    public void Apply(Span<double> window)
    {
        for (int i = window.Length - 1; i >= 1; --i)
            window[i] = window[i] - window[i - 1];
        window[0] = 0;
    }
}
