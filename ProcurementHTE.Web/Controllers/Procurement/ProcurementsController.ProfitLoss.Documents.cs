using System.Security.Claims;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private async Task GenerateProfitLossDocumentsAsync(ProfitLossInputDto dto)
    {
        var procurement =
            await _queryService.GetProcurementByIdAsync(dto.ProcurementId)
            ?? throw new KeyNotFoundException("Procurement tidak ditemukan");

        var docTypes = await GetProcDocumentTypesByNameAsync();
        var pnlDocTypeId = FindRequiredDocumentTypeId(docTypes, "Profit & Loss");
        var pdfBytes = await _documentGenerator.GenerateProfitLossAsync(procurement);

        await SaveGeneratedDocumentAsync(
            dto.ProcurementId,
            pnlDocTypeId,
            pdfBytes,
            $"Profit_Loss_{procurement.ProcNum}.pdf",
            "Profit & Loss auto-generated"
        );

        await GenerateSpmpDocumentAsync(
            procurement,
            docTypes,
            dto.ProcurementId,
            "Surat Perintah Mulai Pekerjaan (SPMP) auto-generated from P&L calculation"
        );
    }

    private async Task GenerateSpmpAfterProfitLossUpdateAsync(string procurementId)
    {
        var procurement =
            await _queryService.GetProcurementByIdAsync(procurementId)
            ?? throw new KeyNotFoundException("Procurement tidak ditemukan");

        await GenerateSpmpDocumentAsync(
            procurement,
            await GetProcDocumentTypesByNameAsync(),
            procurementId,
            "Surat Perintah Mulai Pekerjaan (SPMP) auto-generated from P&L update"
        );
    }

    private async Task GenerateSpmpDocumentAsync(
        Procurement procurement,
        IReadOnlyList<DocumentType> docTypes,
        string procurementId,
        string description
    )
    {
        var spmpDocTypeId = FindDocumentTypeId(
            docTypes,
            "Surat Perintah Mulai Pekerjaan (SPMP)"
        );
        if (string.IsNullOrEmpty(spmpDocTypeId))
            return;

        var spmpPdfBytes = await _documentGenerator.GenerateSPMPAsync(procurement);
        await SaveGeneratedDocumentAsync(
            procurementId,
            spmpDocTypeId,
            spmpPdfBytes,
            $"SPMP_{procurement.ProcNum}.pdf",
            description
        );
    }

    private async Task<IReadOnlyList<DocumentType>> GetProcDocumentTypesByNameAsync()
    {
        var docTypes = await _docTypeService.GetAllDocumentTypesAsync(
            page: 1,
            pageSize: 200,
            search: null,
            fields: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name" },
            ct: default
        );

        return docTypes.Items;
    }

    private async Task SaveGeneratedDocumentAsync(
        string procurementId,
        string documentTypeId,
        byte[] bytes,
        string fileName,
        string description
    )
    {
        await _procDocService.SaveGeneratedAsync(
            new GeneratedProcDocumentRequest
            {
                ProcurementId = procurementId,
                DocumentTypeId = documentTypeId,
                Bytes = bytes,
                FileName = fileName,
                ContentType = "application/pdf",
                Description = description,
                GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.Now,
            }
        );
    }

    private static string FindRequiredDocumentTypeId(
        IReadOnlyList<DocumentType> docTypes,
        string name
    )
    {
        return FindDocumentTypeId(docTypes, name)
            ?? throw new InvalidOperationException($"DocumentType '{name}' tidak ditemukan");
    }

    private static string? FindDocumentTypeId(IReadOnlyList<DocumentType> docTypes, string name)
    {
        return docTypes
            .FirstOrDefault(doc => doc.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?.DocumentTypeId;
    }
}
