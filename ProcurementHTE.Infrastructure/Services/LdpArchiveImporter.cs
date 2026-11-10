using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Services;

public sealed partial class LdpArchiveImporter(AppDbContext db) : ILdpArchiveImporter
{
    private const int FirstColumn = 2;
    private const int LastColumn = 87; // columns past this are helper formulas
    private const int FirstGroupColumn = 33;
    private const int MinFilledCells = 5;

    public LdpImportFile Read(Stream workbook, string fileName, LdpArchiveRange range)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(workbook);
        do
        {
            if (string.Equals(reader.Name, LdpArchiveColumns.Sheet, StringComparison.OrdinalIgnoreCase))
                return ReadSheet(reader, fileName, range);
        } while (reader.NextResult());

        throw new LdpImportException($"Sheet \"{LdpArchiveColumns.Sheet}\" tidak ditemukan di file ini.");
    }

    // Streams the sheet row by row: the workbook is large, and only 86 columns matter.
    private static LdpImportFile ReadSheet(IExcelDataReader reader, string fileName, LdpArchiveRange range)
    {
        object?[]? previous = null;
        Header? header = null;
        var rows = new List<LdpArchiveRow>();
        var excluded = 0;
        var occurrences = new Dictionary<string, int>();
        var rowNumber = 0;

        while (reader.Read())
        {
            rowNumber++;
            var cells = new object?[LastColumn + 1];
            for (var j = 0; j <= LastColumn && j < reader.FieldCount; j++)
                cells[j] = reader.GetValue(j);

            if (header is null)
            {
                if (IsHeaderRow(cells))
                    header = Header.From(previous, cells);
                previous = cells;
                continue;
            }

            if (cells.Count(cell => LdpArchiveColumns.IsFilled(Text(cell))) < MinFilledCells || header.IsPlaceholder(cells))
                continue;

            if (!header.BelongsInArchive(cells, range))
            {
                excluded++;
                continue;
            }

            var row = header.ToRow(cells, rowNumber, range);
            var baseKey = $"{Normalize(row.NoPr)}|{Normalize(row.JobName)}|{row.StartDate:yyyy-MM-dd}";
            occurrences[baseKey] = occurrences.GetValueOrDefault(baseKey) + 1;
            row.SourceKey = $"{Hash(baseKey)}-{occurrences[baseKey]}";
            rows.Add(row);
        }

        if (header is null)
            throw new LdpImportException("Baris judul kolom (Nama Vendor, No. PR) tidak ditemukan di sheet LDP(new).");

        return new LdpImportFile { FileName = fileName, Rows = rows, Excluded = excluded, Range = range };
    }

    private static bool IsHeaderRow(object?[] cells)
    {
        var texts = cells.Select(Text).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return texts.Contains("Nama Vendor") && texts.Contains("No. PR");
    }

    public async Task<LdpImportSummary> PreviewAsync(LdpImportFile file, CancellationToken ct = default)
    {
        var existing = await ExistingAsync(tracked: false, ct);
        return Count(file, existing, apply: null);
    }

    public async Task<LdpImportSummary> ApplyAsync(LdpImportFile file, string actorUserId, CancellationToken ct = default)
    {
        var existing = await ExistingAsync(tracked: true, ct);
        var batch = Clip($"Excel {DateTime.UtcNow:yyyy-MM-dd HH:mm} {file.FileName}", 80)!;
        var now = DateTime.UtcNow;

        var summary = Count(file, existing, apply: (incoming, current) =>
        {
            if (current is null)
            {
                incoming.ImportBatch = batch;
                db.LdpArchiveRows.Add(incoming);
                return;
            }

            // The spreadsheet wins: an Admin's edit or delete gives way to the newest import.
            CopyContent(incoming, current);
            current.ImportBatch = batch;
            current.SourceRow = incoming.SourceRow;
            current.IsDeleted = false;
            current.UpdatedAt = now;
            current.UpdatedByUserId = actorUserId;
        });

        await db.SaveChangesAsync(ct);
        return summary;
    }

    private async Task<Dictionary<string, LdpArchiveRow>> ExistingAsync(bool tracked, CancellationToken ct)
    {
        var query = db.LdpArchiveRows.IgnoreQueryFilters().Where(row => row.SourceKey != null);
        if (!tracked)
            query = query.AsNoTracking();
        return await query.ToDictionaryAsync(row => row.SourceKey!, ct);
    }

    private static LdpImportSummary Count(
        LdpImportFile file,
        IReadOnlyDictionary<string, LdpArchiveRow> existing,
        Action<LdpArchiveRow, LdpArchiveRow?>? apply
    )
    {
        int created = 0, updated = 0, restored = 0, unchanged = 0;
        foreach (var incoming in file.Rows)
        {
            existing.TryGetValue(incoming.SourceKey!, out var current);
            if (current is null)
                created++;
            else if (current.IsDeleted)
                restored++;
            else if (!SameContent(incoming, current))
                updated++;
            else
            {
                unchanged++;
                continue;
            }

            apply?.Invoke(incoming, current);
        }

        return new LdpImportSummary(created, updated, restored, unchanged, file.Excluded);
    }

    private static bool SameContent(LdpArchiveRow a, LdpArchiveRow b) =>
        a.Status == b.Status
        && a.ValuesJson == b.ValuesJson
        && TypedProperties.All(property => Equals(property.GetValue(a), property.GetValue(b)));

    private static void CopyContent(LdpArchiveRow from, LdpArchiveRow to)
    {
        foreach (var property in TypedProperties)
            property.SetValue(to, property.GetValue(from));
        to.Status = from.Status;
        to.ValuesJson = from.ValuesJson;
    }

    private static string Normalize(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..40];

    internal static string Text(object? value) =>
        value switch
        {
            null => string.Empty,
            double number => number.ToString(CultureInfo.InvariantCulture),
            DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ => value.ToString()?.Trim() ?? string.Empty,
        };

    internal static string? Clip(string? value, int length) =>
        value is { Length: > 0 } text && text.Length > length ? text[..length] : (string.IsNullOrEmpty(value) ? null : value);

    internal static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
