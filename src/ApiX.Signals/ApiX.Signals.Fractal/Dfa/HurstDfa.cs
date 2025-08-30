using System.Buffers;

namespace ApiX.Signals.Fractal.Dfa;

/// <summary>
/// 
/// </summary>
public static class HurstDfa
{
    /// <summary>
    /// Detrended Fluctuation Analysis (order 1). Returns (H, R2, usedScales).
    /// x MUST be stationary (returns) and length >= ~64.
    /// </summary>
    public static (double H, double R2, int K) Compute(ReadOnlySpan<double> x)
    {
        int n = x.Length;
        if (n < 64) return (0.5, 0.0, 0);

        // 1) Build profile: cumulative sum of mean-centered series
        double mean = 0;
        for (int i = 0; i < n; i++) mean += x[i];
        mean /= n;

        var profile = ArrayPool<double>.Shared.Rent(n);
        double acc = 0;
        for (int i = 0; i < n; i++) { acc += (x[i] - mean); profile[i] = acc; }

        try
        {
            // 2) Choose box sizes (log-spaced)
            // min scale 4..8, max ~ n/4
            var scales = BuildLogScales(min: 8, max: Math.Max(16, n / 4), count: 8);
            var logs = ArrayPool<double>.Shared.Rent(scales.Length);
            var logF = ArrayPool<double>.Shared.Rent(scales.Length);
            int k = 0;

            for (int si = 0; si < scales.Length; si++)
            {
                int s = scales[si];
                if (s < 4) continue;
                int m = n / s; // segments
                if (m < 2) continue;

                double sumVar = 0;
                // Precompute sums of t and t^2 for linear trend (t=1..s)
                double sumT = s * (s + 1) * 0.5;
                double sumT2 = s * (s + 1) * (2 * s + 1) / 6.0;
                double denom = (s * sumT2 - sumT * sumT); // > 0

                for (int v = 0; v < m; v++)
                {
                    int start = v * s;
                    // Linear least squares: y ~ a*t + b
                    double sumY = 0, sumTY = 0;
                    for (int t = 0; t < s; t++)
                    {
                        double y = profile[start + t];
                        int tt = t + 1;
                        sumY += y;
                        sumTY += tt * y;
                    }
                    double a = (s * sumTY - sumT * sumY) / denom;
                    double b = (sumY - a * sumT) / s;

                    // variance of detrended segment
                    double var = 0;
                    for (int t = 0; t < s; t++)
                    {
                        double fit = a * (t + 1) + b;
                        double d = profile[start + t] - fit;
                        var += d * d;
                    }
                    var /= s;
                    sumVar += var;
                }

                double F = Math.Sqrt(sumVar / m);
                if (F > 0 && !double.IsNaN(F) && !double.IsInfinity(F))
                {
                    logs[k] = Math.Log(s);
                    logF[k] = Math.Log(F);
                    k++;
                }
            }

            if (k < 3) return (0.5, 0.0, k);

            // 3) OLS slope on log-log (slope = H)
            (double slope, double r2) = LinearFit(new ReadOnlySpan<double>(logs, 0, k),
                                                  new ReadOnlySpan<double>(logF, 0, k));
            double H = slope; // DFA-1 slope ≈ H
            H = double.IsFinite(H) ? Math.Clamp(H, 0.0, 1.0) : 0.5;
            return (H, r2, k);
        }
        finally
        {
            ArrayPool<double>.Shared.Return(profile, clearArray: false);
        }
    }

    static int[] BuildLogScales(int min, int max, int count)
    {
        min = Math.Max(4, min);
        max = Math.Max(min + 1, max);
        var scales = new int[count];
        double lmin = Math.Log(min), lmax = Math.Log(max);
        for (int i = 0; i < count; i++)
        {
            double t = i / (double)(count - 1);
            int s = (int)Math.Round(Math.Exp(lmin + t * (lmax - lmin)));
            scales[i] = s;
        }
        // ensure strictly increasing & unique
        for (int i = 1; i < count; i++)
            if (scales[i] <= scales[i - 1]) scales[i] = scales[i - 1] + 1;
        return scales;
    }

    static (double slope, double r2) LinearFit(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
    {
        int n = x.Length;
        double sx = 0, sy = 0, sxx = 0, sxy = 0, syy = 0;
        for (int i = 0; i < n; i++)
        {
            sx += x[i];
            sy += y[i];
            sxx += x[i] * x[i];
            sxy += x[i] * y[i];
            syy += y[i] * y[i];
        }
        double denom = n * sxx - sx * sx;
        if (Math.Abs(denom) < 1e-12) return (0.0, 0.0);
        double slope = (n * sxy - sx * sy) / denom;
        double intercept = (sy - slope * sx) / n;

        // R^2
        double ssTot = 0, ssRes = 0;
        double ybar = sy / n;
        for (int i = 0; i < n; i++)
        {
            double fit = slope * x[i] + intercept;
            ssRes += (y[i] - fit) * (y[i] - fit);
            ssTot += (y[i] - ybar) * (y[i] - ybar);
        }
        double r2 = ssTot <= 0 ? 1.0 : 1.0 - ssRes / ssTot;
        return (slope, Math.Clamp(r2, 0, 1));
    }
}
