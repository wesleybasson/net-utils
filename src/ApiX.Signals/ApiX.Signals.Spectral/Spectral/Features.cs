using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace ApiX.Signals.Spectral.Spectral;

/// <summary>
/// 
/// </summary>
public static class Features
{
    // Hann window
    static void ApplyHann(Span<double> x)
    {
        int n = x.Length;
        for (int i = 0; i < n; i++)
            x[i] *= 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (n - 1));
    }

    // Welch PSD (overlapping segments)
    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="fs"></param>
    /// <param name="segLen"></param>
    /// <param name="overlap"></param>
    /// <returns></returns>
    public static (double[] freq, double[] psd) WelchPsd(ReadOnlySpan<double> x, double fs, int segLen = 128, int overlap = 64)
    {
        int n = x.Length;
        if (segLen > n) segLen = n;
        int step = Math.Max(1, segLen - overlap);
        int segments = 0;

        var acc = new double[segLen / 2 + 1];
        var window = new double[segLen];
        double winNorm = 0;

        for (int i = 0; i < segLen; i++)
        {
            double w = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (segLen - 1));
            window[i] = w;
            winNorm += w * w;
        }

        var buf = new Complex[segLen];

        for (int start = 0; start + segLen <= n; start += step)
        {
            // copy + window
            for (int i = 0; i < segLen; i++)
                buf[i] = new Complex(x[start + i] * window[i], 0.0);

            Fourier.Forward(buf, FourierOptions.Matlab);

            // one-sided power
            for (int k = 0; k <= segLen / 2; k++)
            {
                double re = buf[k].Real, im = buf[k].Imaginary;
                double p = (re * re + im * im);
                acc[k] += p;
            }
            segments++;
        }

        if (segments == 0) return (Array.Empty<double>(), Array.Empty<double>());

        // scale: account for window power and sampling
        double scale = 1.0 / (segments * winNorm * fs);
        var psd = new double[acc.Length];
        var freq = new double[acc.Length];
        for (int k = 0; k < acc.Length; k++)
        {
            // double non-DC/non-Nyquist bins for one-sided PSD
            double factor = (k == 0 || k == acc.Length - 1) ? 1.0 : 2.0;
            psd[k] = acc[k] * factor * scale;
            freq[k] = (fs * k) / segLen;
        }
        return (freq, psd);
    }

    // Fit β on low frequencies of PSD: log(S) ~ -β log(f) + c

    /// <summary>
    /// 
    /// </summary>
    /// <param name="f"></param>
    /// <param name="psd"></param>
    /// <param name="fMaxRatio"></param>
    /// <returns></returns>
    public static (double Beta, double R2) SpectralSlopeBeta(double[] f, double[] psd, double fMaxRatio = 0.1)
    {
        // ignore f=0; take low-frequency band up to fMaxRatio of Nyquist
        double fNyq = f[^1];
        double fMax = fNyq * fMaxRatio;
        var xs = new List<double>();
        var ys = new List<double>();

        for (int i = 1; i < f.Length; i++)
        {
            if (f[i] <= 0 || f[i] > fMax) break;
            if (psd[i] <= 0) continue;
            xs.Add(Math.Log(f[i]));
            ys.Add(Math.Log(psd[i]));
        }
        if (xs.Count < 5) return (0.0, 0.0);

        // linear fit y = a x + b -> beta = -a
        double sx = 0, sy = 0, sxx = 0, sxy = 0, syy = 0;
        int n = xs.Count;
        for (int i = 0; i < n; i++)
        {
            double X = xs[i], Y = ys[i];
            sx += X; sy += Y; sxx += X * X; sxy += X * Y; syy += Y * Y;
        }
        double denom = n * sxx - sx * sx;
        if (Math.Abs(denom) < 1e-12) return (0.0, 0.0);
        double a = (n * sxy - sx * sy) / denom;
        double b = (sy - a * sx) / n;

        // R^2
        double ssTot = 0, ssRes = 0, ybar = sy / n;
        for (int i = 0; i < n; i++)
        {
            double fit = a * xs[i] + b;
            ssRes += (ys[i] - fit) * (ys[i] - fit);
            ssTot += (ys[i] - ybar) * (ys[i] - ybar);
        }
        double r2 = ssTot <= 0 ? 1.0 : 1.0 - ssRes / ssTot;
        double beta = -a;
        return (beta, Math.Clamp(r2, 0, 1));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="f"></param>
    /// <param name="psd"></param>
    /// <returns></returns>
    public static (double Entropy, double Flatness, double TopPeakHz, double PeakPowerRatio) ShapeFeatures(double[] f, double[] psd)
    {
        if (psd.Length == 0) return (0, 0, 0, 0);
        // Normalize to a probability distribution
        double sum = 0; foreach (var p in psd) sum += p;
        double ent = 0, geo = 0;
        double maxP = 0; int maxIdx = 0;
        for (int i = 0; i < psd.Length; i++)
        {
            double p = Math.Max(psd[i], 1e-30);
            double q = p / Math.Max(sum, 1e-30);
            ent += -q * Math.Log(q);
            geo += Math.Log(p);
            if (p > maxP) { maxP = p; maxIdx = i; }
        }
        ent /= Math.Log(psd.Length); // normalize to [0,1]
        double arith = sum / psd.Length;
        double flatness = Math.Exp(geo / psd.Length) / Math.Max(arith, 1e-30);

        // peak power ratio in a small band around the top peak
        int w = Math.Max(1, psd.Length / 100);
        int lo = Math.Max(0, maxIdx - w), hi = Math.Min(psd.Length - 1, maxIdx + w);
        double band = 0;
        for (int i = lo; i <= hi; i++) band += psd[i];
        double peakRatio = band / Math.Max(sum, 1e-30);

        return (ent, flatness, f[maxIdx], peakRatio);
    }
}
