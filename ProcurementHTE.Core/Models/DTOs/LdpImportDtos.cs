using ProcurementHTE.Core.Common;

namespace ProcurementHTE.Core.Models.DTOs;

/// <summary>The rows of one uploaded spreadsheet that belong in the archive.</summary>
public sealed class LdpImportFile
{
    public required string FileName { get; init; }
    public required IReadOnlyList<LdpArchiveRow> Rows { get; init; }

    /// <summary>Jobs left out because they are neither done nor started within <see cref="Range"/>.</summary>
    public int Excluded { get; init; }

    /// <summary>The LDP range the file was read with.</summary>
    public required LdpArchiveRange Range { get; init; }
}

public sealed record LdpImportSummary(int New, int Updated, int Restored, int Unchanged, int Excluded)
{
    public int Changed => New + Updated + Restored;
}

/// <summary>A spreadsheet the importer cannot read. The message is shown to the user.</summary>
public sealed class LdpImportException(string message) : Exception(message);
