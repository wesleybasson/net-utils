namespace ApiX.Data.EFCore.Abstractions;

/// <summary>
/// Optional unit-of-work to coordinate SaveChanges across multiple repositories.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
