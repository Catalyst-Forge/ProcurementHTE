namespace ProcurementHTE.Core.Models.DTOs;

public sealed record ProcurementDocumentGenerationResult(byte[]? PdfBytes, string? ErrorMessage)
{
    public bool Success => PdfBytes is { Length: > 0 };

    public static ProcurementDocumentGenerationResult SuccessResult(byte[] pdfBytes)
    {
        return new ProcurementDocumentGenerationResult(pdfBytes, null);
    }

    public static ProcurementDocumentGenerationResult Unsupported(string? documentTypeName)
    {
        var name = string.IsNullOrWhiteSpace(documentTypeName)
            ? "unknown document"
            : documentTypeName.Trim();

        return new ProcurementDocumentGenerationResult(
            null,
            $"Template for '{name}' is not available yet."
        );
    }
}
