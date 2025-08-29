using ApiX.Abstractions.Domain;

namespace ApiX.Core.Domain;

/// <summary>
/// Base class for domain entities with common auditing and soft-delete metadata.
/// </summary>
public abstract class ModelBase : IIdentifiableEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the entity was created (UTC recommended).
    /// </summary>
    public DateTimeOffset DateCreated { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the entity was last updated (UTC recommended).
    /// </summary>
    public DateTimeOffset DateUpdated { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user or actor who created the entity.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user or actor who last updated the entity.
    /// </summary>
    public Guid UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets optional free-form notes about the entity.
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Gets or sets an optional extensibility field for simple string metadata.
    /// Consider replacing with structured metadata if needs expand.
    /// </summary>
    public string? CustomStringField { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
}
