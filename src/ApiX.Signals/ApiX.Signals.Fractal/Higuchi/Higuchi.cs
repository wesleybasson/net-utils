namespace ApiX.Signals.Fractal.Higuchi;

/// <summary>
/// 
/// </summary>
public static class Higuchi
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="kMax"></param>
    /// <returns></returns>
    public static double FractalDimension(ReadOnlySpan<double> x, int kMax = 8)
    {
        int n = x.Length;
        if (n < (kMax + 2)) return 1.0;

        var logk = new double[kMax];
        var logLk = new double[kMax];
        int kcount = 0;

        for (int k = 1; k <= kMax; k++)
        {
            int mEnd = Math.Min(k, n - 2);
            double sumLm = 0;
            int mCount = 0;

            for (int m = 0; m < mEnd; m++)
            {
                double Lm = 0;
                int prev = m;
                int count = 0;

                // accumulate |x(t) - x(t-k)| along the subsequence
                for (int t = m + k; t < n; t += k)
                {
                    Lm += Math.Abs(x[t] - x[prev]);
                    prev = t;
                    count++;
                }
                if (count <= 0) continue;

                // --- Higuchi normalization (1988) ---
                // L_m(k) = ( (N - 1) / ( count * k ) ) * ( (1.0 / k) * sum |...| )
                // = (N - 1) / (count * k^2) * sum |...|
                double LmNorm = (n - 1.0) / (count * k * k) * Lm;

                sumLm += LmNorm;
                mCount++;
            }

            if (mCount > 0)
            {
                double Lk = sumLm / mCount;
                if (Lk > 0 && double.IsFinite(Lk))
                {
                    logk[kcount] = Math.Log(k);     // <-- log(k)
                    logLk[kcount] = Math.Log(Lk);
                    kcount++;
                }
            }
        }

        if (kcount < 2) return 1.0;

        (double slope, _) = LinearFit(new ReadOnlySpan<double>(logk, 0, kcount),
                                      new ReadOnlySpan<double>(logLk, 0, kcount));
        // For log(L(k)) ~ -D * log(k) + c, FD = -slope
        double fd = -slope;
        return double.IsFinite(fd) ? Math.Clamp(fd, 1.0, 2.0) : 1.5;
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

        // R^2 optional here
        return (slope, 0.0);
    }
}
