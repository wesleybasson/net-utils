using ApiX.Signals.Fractal.Dfa;
using ApiX.Signals.Fractal.Higuchi;

namespace ApiX.Signals.Tests;

public class FractalExtractorsTests
{
    [Fact]
    public void HurstDfa_WhiteNoise_IsAboutHalf()
    {
        // white noise should have H ≈ 0.5
        var x = SignalGenerators.WhiteNoise(512);
        var (H, r2, k) = HurstDfa.Compute(x);
        Assert.InRange(H, 0.40, 0.60);
        Assert.True(k >= 3);
        Assert.InRange(r2, 0.80, 1.01);
    }

    [Fact]
    public void Higuchi_WhiteNoise_NearTwo()
    {
        var x = SignalGenerators.WhiteNoise(1024);
        double fd = Higuchi.FractalDimension(x, kMax: 8);
        Assert.InRange(fd, 1.85, 2.00);
    }

    [Fact]
    public void Higuchi_RandomWalk_AroundOnePointFive()
    {
        var x = SignalGenerators.RandomWalk(2048);
        double fd = Higuchi.FractalDimension(x, kMax: 8);
        Assert.InRange(fd, 1.35, 1.65);
    }

    [Fact]
    public void HurstDfa_AR1_Persistent_AboveHalf()
    {
        // AR(1) with positive phi exhibits persistence
        var x = SignalGenerators.AR1(512, phi: 0.8);
        var (H, _, _) = HurstDfa.Compute(x);
        Assert.InRange(H, 0.60, 0.90);
    }

    [Fact]
    public void HurstDfa_AR1_MeanReverting_BelowHalf()
    {
        // AR(1) with negative phi is anti-persistent
        var x = SignalGenerators.AR1(512, phi: -0.5);
        var (H, _, _) = HurstDfa.Compute(x);
        Assert.InRange(H, 0.30, 0.50);
    }
}
