using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models;

/// <summary>
/// Base entity class that provides soft delete functionality for derived entities.
/// Entities inheriting from this class will be marked as deleted instead of being physically removed from the database.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Indicates whether the entity has been soft-deleted.
    /// Default is false (not deleted).
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// The date and time when the entity was soft-deleted.
    /// Null if the entity has not been deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// The ID of the user who soft-deleted this entity.
    /// Null if the entity has not been deleted.
    /// </summary>
    [StringLength(450)]
    public string? DeletedBy { get; set; }
}
