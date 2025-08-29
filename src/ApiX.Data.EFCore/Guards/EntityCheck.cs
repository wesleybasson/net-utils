using ApiX.Abstractions.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApiX.Data.EFCore.Guards;

/// <summary>
/// 
/// </summary>
public static class EntityCheck
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <param name="dbSet"></param>
    /// <param name="guid"></param>
    /// <returns></returns>
    public static bool EntityGuidExists<TModel>(this DbSet<TModel> dbSet, string guid)
        where TModel : class, IIdentifiableEntity =>
        dbSet.Where(x => x.Id == Guid.Parse(guid)).Any();

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <param name="dbSet"></param>
    /// <param name="guid"></param>
    /// <returns></returns>
    public static bool EntityGuidExists<TModel>(this DbSet<TModel> dbSet, Guid guid)
        where TModel : class, IIdentifiableEntity =>
        dbSet.Where(x => x.Id == guid).Any();
}
