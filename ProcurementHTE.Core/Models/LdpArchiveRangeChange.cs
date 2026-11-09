using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementHTE.Core.Common;

namespace ProcurementHTE.Core.Models;

/// <summary>
/// One change to the range of archived jobs shown in the LDP. The newest row is the range in
/// force; with no rows, <see cref="LdpArchiveRange.Default"/> holds. Append-only: a database
/// trigger refuses UPDATE and DELETE. No foreign keys, and the name is kept as text, so the
/// record outlives the account it mentions.
/// </summary>
public class LdpArchiveRangeChange
{
    public const string AppendOnlyTrigger = "TR_LdpArchiveRangeChanges_AppendOnly";

    [Key]
    [StringLength(450)]
    public string LdpArchiveRangeChangeId { get; set; } = Guid.NewGuid().ToString();

    public DateTime StartFrom { get; set; }

    public DateTime StartTo { get; set; }

    public DateTime PreviousStartFrom { get; set; }

    public DateTime PreviousStartTo { get; set; }

    [Required]
    [StringLength(450)]
    public string ChangedByUserId { get; set; } = default!;

    [Required]
    [StringLength(256)]
    public string ChangedByName { get; set; } = default!;

    /// <summary>UTC.</summary>
    public DateTime ChangedAt { get; set; }

    [NotMapped]
    public LdpArchiveRange Range => new(StartFrom, StartTo);

    [NotMapped]
    public LdpArchiveRange PreviousRange => new(PreviousStartFrom, PreviousStartTo);
}
