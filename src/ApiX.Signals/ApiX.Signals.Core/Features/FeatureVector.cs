namespace ApiX.Signals.Core.Features;

/// <summary>
/// 
/// </summary>
public ref struct FeatureVector
{
    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset AsOf;

    /// <summary>
    /// 
    /// </summary>
    public int N;                 // points in window

    /// <summary>
    /// 
    /// </summary>
    public double FitR2;          // optional (used by DFA/β fits)

    // Common fractal + spectral slots (you can ignore unused)

    /// <summary>
    /// 
    /// </summary>
    public double Hurst;          // [0,1]

    /// <summary>
    /// 
    /// </summary>
    public double HurstDelta;     // relative to median history (if computed upstream)

    /// <summary>
    /// 
    /// </summary>
    public double HiguchiFd;      // [1,2]

    /// <summary>
    /// 
    /// </summary>
    public double Beta;           // spectral slope

    /// <summary>
    /// 
    /// </summary>
    public double SpecEntropy;    // [0,1]

    /// <summary>
    /// 
    /// </summary>
    public double SpecFlatness;   // [0,∞), 1 ~ white, 0 ~ tonal

    /// <summary>
    /// 
    /// </summary>
    public double TopPeakHz;

    /// <summary>
    /// 
    /// </summary>
    public double PeakPowerRatio; // [0,1]

    /// <summary>
    /// 
    /// </summary>
    public double SlopeZ;

    /// <summary>
    /// 
    /// </summary>
    public double VolZ;

    // Small "tag" area for domain consumers

    /// <summary>
    /// 
    /// </summary>
    public uint Flags;            // e.g., bit0: Ready, bit1: StableFit
}
