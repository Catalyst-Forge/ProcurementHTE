namespace ProcurementHTE.Core.Interfaces;

/// <summary>
/// Moves a Docutrax job to Deleted Records together with its documents and P&amp;L, and
/// removes its PR when no other job uses it.
/// </summary>
public interface IProcurementDeletionService
{
    /// <summary>
    /// The number of documents deleted with the job, or null when there is no such job
    /// (already deleted, or never existed).
    /// </summary>
    Task<int?> DeleteAsync(string procurementId, string deletedByUserId);
}
