using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

/// <summary>
/// Loads the LDP spreadsheet into the archive. A row already imported is matched by its
/// source key and replaced by the spreadsheet's version, so imports never duplicate.
/// </summary>
public interface ILdpArchiveImporter
{
    /// <summary>
    /// Reads the sheet. An unfinished job is kept only when it started within <paramref name="range"/>,
    /// and the file remembers that range so the preview and the import that follows agree.
    /// </summary>
    /// <exception cref="LdpImportException">The sheet or one of its columns is missing.</exception>
    LdpImportFile Read(Stream workbook, string fileName, LdpArchiveRange range);

    Task<LdpImportSummary> PreviewAsync(LdpImportFile file, CancellationToken ct = default);

    Task<LdpImportSummary> ApplyAsync(LdpImportFile file, string actorUserId, CancellationToken ct = default);
}
