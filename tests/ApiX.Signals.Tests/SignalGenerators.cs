namespace ApiX.Signals.Tests;

internal static class SignalGenerators
{
    public static double[] WhiteNoise(int n, int seed = 42, double sigma = 1.0)
    {
        var rng = new Random(seed);
        var x = new double[n];
        for (int i = 0; i < n; i++)
            x[i] = NextGaussian(rng) * sigma;
        return x;
    }

    public static double[] RandomWalk(int n, int seed = 43, double stepSigma = 0.5)
    {
        var rng = new Random(seed);
        var x = new double[n];
        double acc = 0;
        for (int i = 0; i < n; i++)
        {
            acc += NextGaussian(rng) * stepSigma;
            x[i] = acc;
        }
        return x;
    }

    public static double[] AR1(int n, double phi, int seed = 44, double sigma = 1.0)
    {
        var rng = new Random(seed);
        var x = new double[n];
        double y = 0;
        for (int i = 0; i < n; i++)
        {
            y = phi * y + NextGaussian(rng) * sigma;
            x[i] = y;
        }
        return x;
    }

    public static double[] SinePlusNoise(int n, double freqHz, double sampleHz, double snr = 3.0, int seed = 45)
    {
        // snr = signal_rms / noise_rms
        var rng = new Random(seed);
        var x = new double[n];
        double w = 2 * Math.PI * freqHz / sampleHz;
        double noiseSigma = 1.0;
        double signalAmp = snr * noiseSigma; // approximate RMS parity
        for (int i = 0; i < n; i++)
            x[i] = signalAmp * Math.Sin(w * i) + NextGaussian(rng) * noiseSigma;
        return x;
    }

    private static double NextGaussian(Random rng)
    {
        // Box-Muller
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
