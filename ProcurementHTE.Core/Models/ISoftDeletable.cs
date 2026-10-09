namespace ProcurementHTE.Core.Models;

/// <summary>
/// An entity that is marked as deleted instead of being removed. EF removals of
/// these entities are turned into soft deletes by the infrastructure layer.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
