namespace ApiX.Signals.Core.Abstractions;

/// <summary>
/// 
/// </summary>
public interface ITransformer
{
    /// <summary>Apply in-place on <paramref name="window"/> (copy if you must).</summary>
    void Apply(Span<double> window);

    /// <summary>
    /// 
    /// </summary>
    string Name { get; }
}
