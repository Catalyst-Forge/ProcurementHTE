using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models;

/// <summary>
/// Append-only record of a permanent delete. It has no foreign keys and keeps names
/// as text, so it outlives the record and the account it mentions.
/// </summary>
public class PermanentDeletionLog
{
    public const string AppendOnlyTrigger = "TR_PermanentDeletionLogs_AppendOnly";

    [Key]
    [StringLength(450)]
    public string PermanentDeletionLogId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(40)]
    public string Kind { get; set; } = default!;

    [Required]
    [StringLength(450)]
    public string RecordId { get; set; } = default!;

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = default!;

    [StringLength(500)]
    public string? Detail { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = default!;

    public int RelatedRows { get; set; }

    public int FileCount { get; set; }

    public int FilesFailed { get; set; }

    [Required]
    [StringLength(450)]
    public string DeletedByUserId { get; set; } = default!;

    [Required]
    [StringLength(256)]
    public string DeletedByName { get; set; } = default!;

    [StringLength(64)]
    public string? IpAddress { get; set; }

    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
