namespace ApiX.Abstractions.Domain;

/// <summary>
/// Defines a contract for entities that can be uniquely identified
/// by a <see cref="Guid"/> identifier.
/// </summary>
public interface IIdentifiableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    Guid Id { get; set; }
}
