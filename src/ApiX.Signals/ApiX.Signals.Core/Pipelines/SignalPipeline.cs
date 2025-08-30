using ApiX.Signals.Core.Abstractions;
using ApiX.Signals.Core.Buffers;
using ApiX.Signals.Core.Features;

namespace ApiX.Signals.Core.Pipelines;

/// <summary>
/// 
/// </summary>
public sealed class SignalPipeline : ISignalPipeline
{
    private readonly RingBuffer<double> _window;
    private readonly List<ITransformer> _transforms = new();
    private readonly List<IFeatureExtractor> _extractors = new();
    private readonly double[] _scratch;
    private readonly Queue<double> _slopeHist = new();
    private readonly Queue<double> _volHist = new();
    private readonly int _histForZ;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="windowSize"></param>
    /// <param name="historyForZ"></param>
    public SignalPipeline(int windowSize, int historyForZ = 60)
    {
        _window = new RingBuffer<double>(windowSize);
        _scratch = new double[windowSize];
        _histForZ = historyForZ;
    }

    /// <inheritdoc />
    public ISignalPipeline Use(ITransformer transformer) { _transforms.Add(transformer); return this; }

    /// <inheritdoc />
    public ISignalPipeline Use(IFeatureExtractor extractor) { _extractors.Add(extractor); return this; }

    /// <inheritdoc />
    public bool TryPush(double value, DateTimeOffset ts, out FeatureVector vec)
    {
        _window.Push(value);
        vec = default;

        if (_window.Count < _window.Capacity) return false;

        // snapshot into scratch
        _window.CopyTo(_scratch);
        var span = _scratch.AsSpan(0, _window.Capacity);

        // transforms (in-place)
        foreach (var t in _transforms) t.Apply(span);

        // basic slope/vol + z-scores (generic)
        (double slope, double vol, double r2) = LinearSlopeVol(span);
        double slopeZ = ZScore(_slopeHist, slope, _histForZ);
        double volZ = ZScore(_volHist, vol, _histForZ);

        vec.AsOf = ts;
        vec.N = span.Length;
        vec.SlopeZ = slopeZ;
        vec.VolZ = volZ;
        vec.FitR2 = r2;

        // extract features
        foreach (var e in _extractors) e.Extract(span, ref vec);

        vec.Flags |= 0x1; // Ready
        return true;
    }

    static (double slope, double vol, double r2) LinearSlopeVol(ReadOnlySpan<double> x)
    {
        int n = x.Length;
        double sx = 0, sy = 0, sxx = 0, sxy = 0, syy = 0;
        for (int i = 0; i < n; i++) { double t = i, y = x[i]; sx += t; sy += y; sxx += t * t; sxy += t * y; syy += y * y; }
        double denom = n * sxx - sx * sx;
        double slope = 0, b = 0, r2 = 0;
        if (Math.Abs(denom) > 1e-12)
        {
            slope = (n * sxy - sx * sy) / denom;
            b = (sy - slope * sx) / n;
            double ssTot = 0, ssRes = 0, ybar = sy / n;
            for (int i = 0; i < n; i++) { double fit = slope * i + b; ssRes += (x[i] - fit) * (x[i] - fit); ssTot += (x[i] - ybar) * (x[i] - ybar); }
            r2 = ssTot <= 0 ? 1 : 1 - ssRes / ssTot;
        }
        // sample std
        double mu = sy / n, v = 0; for (int i = 0; i < n; i++) { double d = x[i] - mu; v += d * d; }
        v /= Math.Max(1, n - 1);
        return (slope, Math.Sqrt(v), r2);
    }

    static double ZScore(Queue<double> q, double v, int max)
    {
        q.Enqueue(v); while (q.Count > max) q.Dequeue();
        if (q.Count < 5) return 0;
        double mu = 0; foreach (var s in q) mu += s; mu /= q.Count;
        double var = 0; foreach (var s in q) { var += (s - mu) * (s - mu); }
        var /= Math.Max(1, q.Count - 1);
        if (var <= 1e-12) return 0;
        return (v - mu) / Math.Sqrt(var);
    }
}
