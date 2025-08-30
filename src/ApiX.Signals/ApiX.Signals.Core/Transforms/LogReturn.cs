using ApiX.Signals.Core.Abstractions;

namespace ApiX.Signals.Core.Transforms;

/// <summary>
/// 
/// </summary>
public sealed class LogReturn : ITransformer
{
    /// <inheritdoc/>
    public string Name => "logret";

    /// <inheritdoc/>
    public void Apply(Span<double> window)
    {
        for (int i = window.Length - 1; i >= 1; --i)
            window[i] = Math.Log(Math.Max(1e-12, window[i])) - Math.Log(Math.Max(1e-12, window[i - 1]));
        window[0] = 0;
    }
}
