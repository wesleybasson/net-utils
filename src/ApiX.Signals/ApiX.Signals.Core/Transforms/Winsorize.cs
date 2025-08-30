using ApiX.Signals.Core.Abstractions;

namespace ApiX.Signals.Core.Transforms;

/// <summary>
/// 
/// </summary>
public sealed class Winsorize : ITransformer
{
    /// <inheritdoc/>
    public string Name => "winsor";
    private readonly double _pLow, _pHigh;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pLow"></param>
    /// <param name="pHigh"></param>
    public Winsorize(double pLow = 0.01, double pHigh = 0.99) { _pLow = pLow; _pHigh = pHigh; }

    /// <inheritdoc/>
    public void Apply(Span<double> window)
    {
        // copy to temp, sort once
        var tmp = window.ToArray();
        Array.Sort(tmp);
        double lo = Quantile(tmp, _pLow);
        double hi = Quantile(tmp, _pHigh);
        for (int i = 0; i < window.Length; i++)
            window[i] = Math.Min(hi, Math.Max(lo, window[i]));
    }

    static double Quantile(double[] sorted, double p)
    {
        double idx = p * (sorted.Length - 1);
        int i = (int)Math.Floor(idx);
        double frac = idx - i;
        if (i + 1 >= sorted.Length) return sorted[^1];
        return sorted[i] * (1 - frac) + sorted[i + 1] * frac;
    }
}
