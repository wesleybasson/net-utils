namespace ApiX.Signals.Spectral.Extractors;

/// <summary>
/// Minimal contract for sharing the most recently computed PSD (Welch) for the
/// current pipeline window, so other extractors can reuse it and avoid re-FFT.
/// </summary>
public interface IPsdProvider
{
    /// <summary>
    /// Try to get the cached one-sided PSD for the current window.
    /// Returns <c>null</c> if no PSD is available for this tick.
    /// </summary>
    (double[] freq, double[] psd)? TryGet(ReadOnlySpan<double> window);
}
