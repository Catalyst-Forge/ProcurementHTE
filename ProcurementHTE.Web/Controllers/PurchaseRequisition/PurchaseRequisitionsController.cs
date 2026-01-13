using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.PR
{
    [Authorize(Roles = "Admin, AP-PO")]
    public class PurchaseRequisitionsController : Controller
    {
        private const string ActivePageName = "Index PR Service";
        private readonly IProcurementRepository _procurementRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IProfitLossRepository _profitLossRepository;
        private readonly IPurchaseRequisitionService _purchaseRequisitionService;
        private readonly IProcurementDocumentQuery _procurementDocumentQuery;
        private readonly IProcDocumentService _procDocumentService;
        private readonly IVendorRoundLetterRepository _vendorRoundLetterRepository;
        private readonly IDocumentGenerator _documentGenerator;
        private readonly IDocumentTypeRepository _documentTypeRepository;
        private readonly IProcurementService _procurementService;
        private readonly IObjectStorage _objectStorage;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IProcurementTrackingService _procurementTrackingService;

        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
        private static readonly string[] AllowedExtensions =
        [
            ".pdf",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
        ];

        public PurchaseRequisitionsController(
            IProcurementRepository procurementRepository,
            IVendorRepository vendorRepository,
            IProfitLossRepository profitLossRepository,
            IPurchaseRequisitionService purchaseRequisitionService,
            IProcurementDocumentQuery procurementDocumentQuery,
            IProcDocumentService procDocumentService,
            IVendorRoundLetterRepository vendorRoundLetterRepository,
            IDocumentGenerator documentGenerator,
            IDocumentTypeRepository documentTypeRepository,
            IProcurementService procurementService,
            IObjectStorage objectStorage,
            IOptions<ObjectStorageOptions> storageOptions,
            IHttpClientFactory httpClientFactory,
            IProcurementTrackingService procurementTrackingService
        )
        {
            _procurementRepository = procurementRepository;
            _vendorRepository = vendorRepository;
            _profitLossRepository = profitLossRepository;
            _purchaseRequisitionService = purchaseRequisitionService;
            _procurementDocumentQuery = procurementDocumentQuery;
            _procDocumentService = procDocumentService;
            _vendorRoundLetterRepository = vendorRoundLetterRepository;
            _documentGenerator = documentGenerator;
            _documentTypeRepository = documentTypeRepository;
            _procurementService = procurementService;
            _objectStorage = objectStorage;
            _storageOptions =
                storageOptions?.Value ?? throw new ArgumentNullException(nameof(storageOptions));
            _httpClientFactory = httpClientFactory;
            _procurementTrackingService = procurementTrackingService;
        }

        // GET: PurchaseRequisitionsController
        public async Task<ActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string? search = null
        )
        {
            var fields = new HashSet<string> { "PrNumber", "Description" };
            var result = await _purchaseRequisitionService.GetAllAsync(
                page,
                pageSize,
                search,
                fields
            );

            var viewModels = result
                .Items.Select(pr => new PurchaseRequisitionListViewModel
                {
                    PrId = pr.PrId,
                    PrNumber = pr.PrNumber,
                    RequestDate = pr.RequestDate,
                    Description = pr.Description,
                    DocumentFileName = pr.DocumentFileName,
                    ProcurementCount = pr.Procurements?.Count ?? 0,
                    CreatedByUserId = pr.CreatedByUserId,
                    CreatedByUserName = pr.CreatedByUser?.FullName,
                    CreatedAt = pr.CreatedAt,
                })
                .ToList();

            var pagedResult = new Core.Common.PagedResult<PurchaseRequisitionListViewModel>
            {
                Items = viewModels,
                Page = result.Page,
                PageSize = result.PageSize,
                Total = result.Total,
            };

            return View(pagedResult);
        }

        // GET: PurchaseRequisitionsController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var pr = await _purchaseRequisitionService.GetByIdWithProcurementsAsync(id);
            if (pr == null)
            {
                return NotFound();
            }

            var viewModel = new PurchaseRequisitionDetailsViewModel
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                RequestDate = pr.RequestDate,
                Description = pr.Description,
                DocumentFileName = pr.DocumentFileName,
                DocumentFilePath = pr.DocumentFilePath,
                DocumentFileSize = pr.DocumentFileSize,
                CreatedByUserId = pr.CreatedByUserId,
                CreatedByUserName = pr.CreatedByUser?.FullName ?? pr.CreatedByUser?.UserName,
                CreatedAt = pr.CreatedAt,
                UpdatedAt = pr.UpdatedAt,
            };

            // Load required documents and round letters for each procurement
            foreach (var procurement in pr.Procurements ?? [])
            {
                var requiredDocs = await _procurementDocumentQuery.GetRequiredDocsAsync(
                    procurement.ProcurementId
                );
                var roundLetters = await _vendorRoundLetterRepository.ListByProcurementAsync(
                    procurement.ProcurementId
                );

                // Get all vendor names from ProfitLosses
                var vendorNames =
                    procurement
                        .ProfitLosses?.Where(pl => pl.SelectedVendor != null)
                        .Select(pl => pl.SelectedVendor!.VendorName)
                        .Distinct()
                        .ToList() ?? [];

                var procVm = new ProcurementWithDocsViewModel
                {
                    ProcurementId = procurement.ProcurementId,
                    ProcNum = procurement.ProcNum,
                    Wonum = procurement.Wonum,
                    JobName = procurement.JobName,
                    JobTypeName = procurement.JobType?.TypeName,
                    StatusName = procurement.Status?.StatusName,
                    Category = procurement.ProcurementCategory.ToString(),
                    StartDate = procurement.StartDate,
                    EndDate = procurement.EndDate,
                    VendorName = vendorNames.Any() ? string.Join(", ", vendorNames) : null,
                    RequiredDocuments = requiredDocs?.Items ?? [],
                    RoundLetters = roundLetters?.ToList() ?? [],
                    CompletedDocs = requiredDocs?.Items?.Count(d => d.Uploaded) ?? 0,
                    TotalDocs = requiredDocs?.Items?.Count ?? 0,
                };

                viewModel.Procurements.Add(procVm);
            }

            // Fetch Procurement Tracking data for all procurements
            var procurementTrackings = new List<ProcurementTrackingDto>();
            foreach (var proc in viewModel.Procurements)
            {
                var procTracking = await _procurementTrackingService.GetTrackingByProcurementIdAsync(
                    proc.ProcurementId,
                    HttpContext.RequestAborted
                );
                if (procTracking != null)
                {
                    procurementTrackings.Add(procTracking);
                }
            }
            ViewData["ProcurementTrackings"] = procurementTrackings;

            return View(viewModel);
        }

        // GET: PurchaseRequisitionsController/Create
        public async Task<ActionResult> Create()
        {
            await PopulateViewBagForCreate();
            return View(new PurchaseRequisitionCreateViewModel());
        }

        // POST: PurchaseRequisitionsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(PurchaseRequisitionCreateViewModel model)
        {
            // Validate at least one procurement is selected
            if (model.ProcurementIds == null || model.ProcurementIds.Count == 0)
            {
                ModelState.AddModelError(
                    "ProcurementIds",
                    "At least one procurement must be selected."
                );
            }
            if (!ModelState.IsValid)
            {
                await PopulateViewBagForCreate();
                return View(model);
            }

            // Validate file
            if (model.DocumentFile == null || model.DocumentFile.Length == 0)
            {
                ModelState.AddModelError("DocumentFile", "Please upload a document file.");
                await PopulateViewBagForCreate();
                return View(model);
            }

            if (model.DocumentFile.Length > MaxFileSize)
            {
                ModelState.AddModelError("DocumentFile", "File size exceeds 10MB limit.");
                await PopulateViewBagForCreate();
                return View(model);
            }

            var fileExtension = Path.GetExtension(model.DocumentFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError(
                    "DocumentFile",
                    "Invalid file type. Allowed: PDF, DOC, DOCX, XLS, XLSX."
                );
                await PopulateViewBagForCreate();
                return View(model);
            }

            // Validate PR Number uniqueness
            if (!string.IsNullOrWhiteSpace(model.PRNumber))
            {
                var prExists = await _purchaseRequisitionService.IsPrNumberExistsAsync(model.PRNumber);
                if (prExists)
                {
                    ModelState.AddModelError("PRNumber", $"PR Number '{model.PRNumber}' sudah digunakan. Gunakan nomor lain.");
                    await PopulateViewBagForCreate();
                    return View(model);
                }
            }

            try
            {
                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Challenge();
                }

                // Build object key for MinIO
                var prId = Guid.NewGuid().ToString();
                var objectKey = BuildPrDocumentObjectKey(prId, model.DocumentFile.FileName);

                // Upload to object storage (MinIO)
                await using var stream = model.DocumentFile.OpenReadStream();
                await _objectStorage.UploadAsync(
                    _storageOptions.Bucket,
                    objectKey,
                    stream,
                    model.DocumentFile.Length,
                    model.DocumentFile.ContentType,
                    HttpContext.RequestAborted
                );

                // Create PurchaseRequisition entity
                var purchaseRequisition = new PurchaseRequisition
                {
                    PrId = prId,
                    PrNumber = model.PRNumber,
                    RequestDate = model.RequestDate,
                    Description = model.Description,
                    DocumentFileName = model.DocumentFile.FileName,
                    DocumentFilePath = objectKey, // Store object key instead of file path
                    DocumentContentType = model.DocumentFile.ContentType,
                    DocumentFileSize = model.DocumentFile.Length,
                    CreatedByUserId = userId,
                };

                // Save to database with linked procurements
                await _purchaseRequisitionService.CreateAsync(
                    purchaseRequisition,
                    model.ProcurementIds ?? []
                );

                TempData["SuccessMessage"] =
                    $"Purchase Requisition {purchaseRequisition.PrNumber} created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                await PopulateViewBagForCreate();
                return View(model);
            }
        }

        // GET: PurchaseRequisitionsController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var pr = await _purchaseRequisitionService.GetByIdWithProcurementsAsync(id);
            if (pr == null)
            {
                return NotFound();
            }

            // Authorization check: Only Admin or Creator can edit
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && pr.CreatedByUserId != currentUserId)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit PR ini.";
                return RedirectToAction(nameof(Index));
            }

            var model = new PurchaseRequisitionEditViewModel
            {
                PrId = pr.PrId,
                PRNumber = pr.PrNumber,
                RequestDate = pr.RequestDate,
                Description = pr.Description,
                ExistingDocumentFileName = pr.DocumentFileName,
                ProcurementIds =
                    pr.Procurements?.Select(p => p.ProcurementId).ToList() ?? new List<string>(),
            };

            await PopulateViewBagForCreate();
            return View(model);
        }

        // POST: PurchaseRequisitionsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, PurchaseRequisitionEditViewModel model)
        {
            if (id != model.PrId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await PopulateViewBagForCreate();
                return View(model);
            }

            // Validate PR Number uniqueness (exclude current PR)
            if (!string.IsNullOrWhiteSpace(model.PRNumber))
            {
                var prExists = await _purchaseRequisitionService.IsPrNumberExistsAsync(model.PRNumber, model.PrId);
                if (prExists)
                {
                    ModelState.AddModelError("PRNumber", $"PR Number '{model.PRNumber}' sudah digunakan. Gunakan nomor lain.");
                    await PopulateViewBagForCreate();
                    return View(model);
                }
            }

            try
            {
                var existingPr = await _purchaseRequisitionService.GetByIdAsync(id);
                if (existingPr == null)
                {
                    return NotFound();
                }

                // Authorization check: Only Admin or Creator can edit
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");
                if (!isAdmin && existingPr.CreatedByUserId != currentUserId)
                {
                    TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit PR ini.";
                    return RedirectToAction(nameof(Index));
                }

                // Update basic properties
                existingPr.PrNumber = model.PRNumber;
                existingPr.RequestDate = model.RequestDate;
                existingPr.Description = model.Description;

                // Handle file upload if new file provided
                if (model.DocumentFile != null && model.DocumentFile.Length > 0)
                {
                    if (model.DocumentFile.Length > MaxFileSize)
                    {
                        ModelState.AddModelError("DocumentFile", "File size exceeds 10MB limit.");
                        await PopulateViewBagForCreate();
                        return View(model);
                    }

                    var fileExtension = Path.GetExtension(model.DocumentFile.FileName)
                        .ToLowerInvariant();
                    if (!AllowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError(
                            "DocumentFile",
                            "Invalid file type. Allowed: PDF, DOC, DOCX, XLS, XLSX."
                        );
                        await PopulateViewBagForCreate();
                        return View(model);
                    }

                    // Delete old file from object storage if exists
                    if (!string.IsNullOrEmpty(existingPr.DocumentFilePath))
                    {
                        await SafeDeleteFromStorageAsync(existingPr.DocumentFilePath);
                    }

                    // Build new object key for MinIO
                    var objectKey = BuildPrDocumentObjectKey(
                        existingPr.PrId,
                        model.DocumentFile.FileName
                    );

                    // Upload to object storage (MinIO)
                    await using var stream = model.DocumentFile.OpenReadStream();
                    await _objectStorage.UploadAsync(
                        _storageOptions.Bucket,
                        objectKey,
                        stream,
                        model.DocumentFile.Length,
                        model.DocumentFile.ContentType,
                        HttpContext.RequestAborted
                    );

                    existingPr.DocumentFileName = model.DocumentFile.FileName;
                    existingPr.DocumentFilePath = objectKey; // Store object key
                    existingPr.DocumentContentType = model.DocumentFile.ContentType;
                    existingPr.DocumentFileSize = model.DocumentFile.Length;
                }

                await _purchaseRequisitionService.UpdateAsync(existingPr, model.ProcurementIds);

                TempData["SuccessMessage"] =
                    $"Purchase Requisition {existingPr.PrNumber} updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                await PopulateViewBagForCreate();
                return View(model);
            }
        }

        // GET: PurchaseRequisitionsController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var pr = await _purchaseRequisitionService.GetByIdWithProcurementsAsync(id);
            if (pr == null)
            {
                return NotFound();
            }

            // Authorization check: Only Admin or Creator can delete
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && pr.CreatedByUserId != currentUserId)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk menghapus PR ini.";
                return RedirectToAction(nameof(Index));
            }

            return View(pr);
        }

        // POST: PurchaseRequisitionsController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var pr = await _purchaseRequisitionService.GetByIdAsync(id);
                if (pr == null)
                {
                    return NotFound();
                }

                // Authorization check: Only Admin or Creator can delete
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var isAdmin = User.IsInRole("Admin");
                if (!isAdmin && pr.CreatedByUserId != currentUserId)
                {
                    TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk menghapus PR ini.";
                    return RedirectToAction(nameof(Index));
                }

                // Delete file from object storage if exists
                if (!string.IsNullOrEmpty(pr.DocumentFilePath))
                {
                    await SafeDeleteFromStorageAsync(pr.DocumentFilePath);
                }

                await _purchaseRequisitionService.DeleteAsync(id, currentUserId);

                TempData["SuccessMessage"] =
                    $"Purchase Requisition {pr.PrNumber} deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: PurchaseRequisitionsController/DownloadDocument/5
        [HttpGet]
        public async Task<ActionResult> DownloadDocument(string id)
        {
            var pr = await _purchaseRequisitionService.GetByIdAsync(id);
            if (pr == null || string.IsNullOrEmpty(pr.DocumentFilePath))
            {
                return NotFound();
            }

            try
            {
                // Get presigned URL from object storage
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["response-content-disposition"] =
                        $"attachment; filename=\"{Uri.EscapeDataString(pr.DocumentFileName ?? "document")}\"",
                    ["response-content-type"] =
                        pr.DocumentContentType ?? "application/octet-stream",
                };

                var url = await _objectStorage.GetPresignedUrlHeaderAsync(
                    _storageOptions.Bucket,
                    pr.DocumentFilePath,
                    TimeSpan.FromMinutes(30),
                    headers,
                    HttpContext.RequestAborted
                );

                // Download from MinIO via HTTP client
                var client = _httpClientFactory.CreateClient("MinioProxy");
                var response = await client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    HttpContext.RequestAborted
                );

                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync(HttpContext.RequestAborted);

                var contentType = string.IsNullOrWhiteSpace(pr.DocumentContentType)
                    ? "application/octet-stream"
                    : pr.DocumentContentType;

                return File(
                    stream,
                    contentType,
                    fileDownloadName: pr.DocumentFileName ?? "document",
                    enableRangeProcessing: true
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to download document: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: PurchaseRequisitionsController/PreviewDocument/5
        [HttpGet]
        public async Task<IActionResult> PreviewDocumentUrl(string id)
        {
            var pr = await _purchaseRequisitionService.GetByIdAsync(id);
            if (pr == null || string.IsNullOrEmpty(pr.DocumentFilePath))
            {
                return Json(new { ok = false, error = "Document not found." });
            }

            try
            {
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(pr.DocumentFileName))
                {
                    headers["response-content-disposition"] =
                        $"inline; filename=\"{Uri.EscapeDataString(pr.DocumentFileName)}\"";
                }
                if (!string.IsNullOrWhiteSpace(pr.DocumentContentType))
                {
                    headers["response-content-type"] = pr.DocumentContentType;
                }

                var url = await _objectStorage.GetPresignedUrlHeaderAsync(
                    _storageOptions.Bucket,
                    pr.DocumentFilePath,
                    TimeSpan.FromMinutes(15),
                    headers,
                    HttpContext.RequestAborted
                );

                return Json(new { ok = true, url });
            }
            catch (Exception ex)
            {
                return Json(
                    new { ok = false, error = $"Failed to create preview link: {ex.Message}" }
                );
            }
        }

        // POST: PurchaseRequisitionsController/UploadProcurementDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProcurementDocument(
            string prId,
            string procurementId,
            string documentTypeId,
            IFormFile documentFile
        )
        {
            try
            {
                if (documentFile == null || documentFile.Length == 0)
                {
                    return Json(new { ok = false, error = "Please select a file to upload." });
                }

                if (documentFile.Length > MaxFileSize)
                {
                    return Json(new { ok = false, error = "File size exceeds 10MB limit." });
                }

                var fileExtension = Path.GetExtension(documentFile.FileName).ToLowerInvariant();
                if (fileExtension != ".pdf")
                {
                    return Json(new { ok = false, error = "Only PDF files are allowed." });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { ok = false, error = "User not authenticated." });
                }

                // Create stream from file
                await using var stream = documentFile.OpenReadStream();

                // Upload using ProcDocumentService
                var request = new Core.Models.DTOs.UploadProcDocumentRequest
                {
                    ProcurementId = procurementId,
                    DocumentTypeId = documentTypeId,
                    FileName = documentFile.FileName,
                    ContentType = documentFile.ContentType,
                    Content = stream,
                    Size = documentFile.Length,
                    Description = $"Uploaded via PR Service ({prId})",
                    UploadedByUserId = userId,
                    NowUtc = DateTime.UtcNow,
                };

                var result = await _procDocumentService.UploadAsync(request);

                return Json(
                    new
                    {
                        ok = true,
                        message = "Document uploaded successfully.",
                        document = new
                        {
                            id = result.ProcDocumentId,
                            name = result.FileName,
                            size = result.Size,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // POST: PurchaseRequisitionsController/GenerateDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateDocument(
            string prId,
            string procurementId,
            string documentTypeId,
            string? procDocumentId
        )
        {
            try
            {
                var procurement = await _procurementService.GetProcurementByIdAsync(procurementId);
                if (procurement == null)
                {
                    TempData["ErrorMessage"] = "Procurement not found.";
                    return RedirectToAction(nameof(Details), new { id = prId });
                }

                var docType = await _documentTypeRepository.GetByIdAsync(documentTypeId);
                if (docType == null)
                {
                    TempData["ErrorMessage"] = "Document type not found.";
                    return RedirectToAction(nameof(Details), new { id = prId });
                }

                // Generate PDF based on document type name
                byte[] pdfBytes = docType.Name switch
                {
                    "Memorandum" => await _documentGenerator.GenerateMemorandumAsync(procurement),
                    "Permintaan Pekerjaan" =>
                        await _documentGenerator.GeneratePermintaanPekerjaanAsync(procurement),
                    "Service Order" => await _documentGenerator.GenerateServiceOrderAsync(
                        procurement
                    ),
                    "Market Survey" => await _documentGenerator.GenerateMarketSurveyAsync(
                        procurement
                    ),
                    "Surat Perintah Mulai Pekerjaan (SPMP)" =>
                        await _documentGenerator.GenerateSPMPAsync(procurement),
                    "Surat Penawaran Harga" =>
                        await _documentGenerator.GenerateSuratPenawaranHargaAsync(procurement),
                    "Surat Negosiasi Harga" =>
                        await _documentGenerator.GenerateSuratNegosiasiHargaAsync(procurement),
                    "Rencana Kerja dan Syarat-Syarat (RKS)" =>
                        await _documentGenerator.GenerateRKSAsync(procurement),
                    "Risk Assessment (RA)" => await _documentGenerator.GenerateRiskAssessmentAsync(
                        procurement
                    ),
                    "Owner Estimate (OE)" => await _documentGenerator.GenerateOwnerEstimateAsync(
                        procurement
                    ),
                    "Bill of Quantity (BOQ)" => await _documentGenerator.GenerateBOQAsync(
                        procurement
                    ),
                    "Profit & Loss" => await _documentGenerator.GenerateProfitLossAsync(
                        procurement
                    ),
                    "Justifikasi" => await _documentGenerator.GenerateJustifikasiAsync(procurement),
                    _ => throw new NotImplementedException(
                        $"Template for '{docType.Name}' is not available yet."
                    ),
                };

                // Save to MinIO
                var result = await _procDocumentService.SaveGeneratedAsync(
                    new GeneratedProcDocumentRequest
                    {
                        ProcurementId = procurementId,
                        DocumentTypeId = documentTypeId,
                        Bytes = pdfBytes,
                        FileName = $"{docType.Name}.pdf",
                        ContentType = "application/pdf",
                        Description =
                            $"Generated from PR Service on {DateTime.Now:dd MMM yyyy HH:mm}",
                        CreatedAt = DateTime.UtcNow,
                        GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        ProcDocumentId = procDocumentId,
                    }
                );

                TempData["SuccessMessage"] = $"Document '{docType.Name}' generated successfully!";
            }
            catch (NotImplementedException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to generate document: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = prId });
        }

        // AJAX: Generate Document without page refresh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateDocumentAjax(
            string prId,
            string procurementId,
            string documentTypeId,
            string? procDocumentId
        )
        {
            try
            {
                var procurement = await _procurementService.GetProcurementByIdAsync(procurementId);
                if (procurement == null)
                {
                    return Json(new { success = false, message = "Procurement not found." });
                }

                var docType = await _documentTypeRepository.GetByIdAsync(documentTypeId);
                if (docType == null)
                {
                    return Json(new { success = false, message = "Document type not found." });
                }

                // Generate PDF based on document type name
                byte[] pdfBytes = docType.Name switch
                {
                    "Memorandum" => await _documentGenerator.GenerateMemorandumAsync(procurement),
                    "Permintaan Pekerjaan" =>
                        await _documentGenerator.GeneratePermintaanPekerjaanAsync(procurement),
                    "Service Order" => await _documentGenerator.GenerateServiceOrderAsync(
                        procurement
                    ),
                    "Market Survey" => await _documentGenerator.GenerateMarketSurveyAsync(
                        procurement
                    ),
                    "Surat Perintah Mulai Pekerjaan (SPMP)" =>
                        await _documentGenerator.GenerateSPMPAsync(procurement),
                    "Surat Penawaran Harga" =>
                        await _documentGenerator.GenerateSuratPenawaranHargaAsync(procurement),
                    "Surat Negosiasi Harga" =>
                        await _documentGenerator.GenerateSuratNegosiasiHargaAsync(procurement),
                    "Rencana Kerja dan Syarat-Syarat (RKS)" =>
                        await _documentGenerator.GenerateRKSAsync(procurement),
                    "Risk Assessment (RA)" => await _documentGenerator.GenerateRiskAssessmentAsync(
                        procurement
                    ),
                    "Owner Estimate (OE)" => await _documentGenerator.GenerateOwnerEstimateAsync(
                        procurement
                    ),
                    "Bill of Quantity (BOQ)" => await _documentGenerator.GenerateBOQAsync(
                        procurement
                    ),
                    "Profit & Loss" => await _documentGenerator.GenerateProfitLossAsync(
                        procurement
                    ),
                    "Justifikasi" => await _documentGenerator.GenerateJustifikasiAsync(procurement),
                    _ => throw new NotImplementedException(
                        $"Template for '{docType.Name}' is not available yet."
                    ),
                };

                // Save to MinIO
                var result = await _procDocumentService.SaveGeneratedAsync(
                    new GeneratedProcDocumentRequest
                    {
                        ProcurementId = procurementId,
                        DocumentTypeId = documentTypeId,
                        Bytes = pdfBytes,
                        FileName = $"{docType.Name}.pdf",
                        ContentType = "application/pdf",
                        Description =
                            $"Generated from PR Service on {DateTime.Now:dd MMM yyyy HH:mm}",
                        CreatedAt = DateTime.UtcNow,
                        GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        ProcDocumentId = procDocumentId,
                    }
                );

                return Json(new { 
                    success = true, 
                    message = $"Document '{docType.Name}' generated successfully!",
                    procDocumentId = result.ProcDocumentId,
                    fileName = result.FileName,
                    documentTypeName = docType.Name
                });
            }
            catch (NotImplementedException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to generate document: {ex.Message}" });
            }
        }

        // API: Get filtered procurements
        [HttpGet]
        public async Task<JsonResult> GetFilteredProcurements(
            string? vendorId = null,
            int? category = null,
            string? jobTypeId = null
        )
        {
            var procurements = await _procurementRepository.GetAllForSelectionAsync();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Filter procurements that don't have PR linked yet (PrId is null)
            // and have status "In Progress" (procurement that has been approved by AP-PO)
            // and have AppoUserId filled (procurement that has been accepted by AP-PO)
            procurements = procurements
                .Where(p => p.PrId == null) // Only procurements not yet linked to a PR
                .Where(p =>
                    p.Status != null
                    && string.Equals(
                        p.Status.StatusName,
                        "In Progress",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Where(p => !string.IsNullOrWhiteSpace(p.AppoUserId)) // Only approved procurements
                .ToList();

            // If user is AP-PO (not Admin), filter by AppoUserId
            if (User.IsInRole("AP-PO") && !User.IsInRole("Admin"))
            {
                procurements = procurements.Where(p => p.AppoUserId == currentUserId).ToList();
            }

            // Apply filters
            if (!string.IsNullOrEmpty(vendorId))
            {
                procurements = procurements
                    .Where(p =>
                        p.ProfitLosses != null
                        && p.ProfitLosses.Any(pl => pl.SelectedVendorId == vendorId)
                    )
                    .ToList();
            }

            if (category.HasValue)
            {
                var categoryEnum = (ProcurementCategory)category.Value;
                procurements = procurements
                    .Where(p => p.ProcurementCategory == categoryEnum)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(jobTypeId))
            {
                procurements = procurements.Where(p => p.JobTypeId == jobTypeId).ToList();
            }

            var result = procurements
                .Select(p => new
                {
                    id = p.ProcurementId,
                    procNum = p.ProcNum,
                    wonum = p.Wonum,
                    jobName = p.JobName,
                    category = p.ProcurementCategory.ToString(),
                    categoryInt = (int)p.ProcurementCategory,
                    jobType = p.JobType?.TypeName ?? "-",
                    jobTypeId = p.JobTypeId,
                    status = p.Status?.StatusName ?? "Created",
                    startDate = p.StartDate.ToString("yyyy-MM-dd"),
                    vendorId = p.ProfitLosses?.FirstOrDefault()?.SelectedVendorId ?? "",
                    vendorName = p.ProfitLosses?.FirstOrDefault()?.SelectedVendor?.VendorName
                        ?? "-",
                })
                .ToList();

            return Json(result);
        }

        // API: Get procurements linked to a specific PR (for Edit page)
        [HttpGet]
        public async Task<JsonResult> GetLinkedProcurements(string prId)
        {
            if (string.IsNullOrEmpty(prId))
            {
                return Json(Array.Empty<object>());
            }

            var pr = await _purchaseRequisitionService.GetByIdWithProcurementsAsync(prId);
            if (pr == null || pr.Procurements == null || !pr.Procurements.Any())
            {
                return Json(Array.Empty<object>());
            }

            var result = pr
                .Procurements.Select(p => new
                {
                    id = p.ProcurementId,
                    procNum = p.ProcNum,
                    wonum = p.Wonum,
                    jobName = p.JobName,
                    category = p.ProcurementCategory.ToString(),
                    categoryInt = (int)p.ProcurementCategory,
                    jobType = p.JobType?.TypeName ?? "-",
                    jobTypeId = p.JobTypeId,
                    status = p.Status?.StatusName ?? "Unknown",
                    startDate = p.StartDate.ToString("yyyy-MM-dd"),
                    vendorId = p.ProfitLosses?.FirstOrDefault()?.SelectedVendorId ?? "",
                    vendorName = p.ProfitLosses?.FirstOrDefault()?.SelectedVendor?.VendorName
                        ?? "-",
                })
                .ToList();

            return Json(result);
        }

        // POST: PurchaseRequisitions/SendApproval
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendApproval(string procurementId, string prId, CancellationToken ct)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User tidak teridentifikasi.";
                return RedirectToAction(nameof(Details), new { id = prId });
            }

            var result = await _procurementTrackingService.SendForApprovalAsync(procurementId, userId, ct);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id = prId });
        }

        #region Private Helper Methods

        private async Task PopulateViewBagForCreate()
        {
            // Get all vendors
            var vendors = await _vendorRepository.GetAllAsync();
            ViewBag.Vendors = vendors
                .Select(v => new SelectListItem { Value = v.VendorId, Text = v.VendorName })
                .ToList();

            // Get all job types
            var jobTypes = await _procurementRepository.GetJobTypesAsync();
            ViewBag.JobTypes = jobTypes
                .Select(j => new SelectListItem { Value = j.JobTypeId, Text = j.TypeName })
                .ToList();

            // Get procurement categories from enum
            ViewBag.Categories = Enum.GetValues(typeof(ProcurementCategory))
                .Cast<ProcurementCategory>()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToString(),
                })
                .ToList();
        }

        private static string BuildPrDocumentObjectKey(string prId, string fileName)
        {
            var sanitized = SanitizeFileName(fileName);
            return $"purchase-requisitions/{prId}/documents/{Guid.NewGuid():N}-{sanitized}";
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "file.dat";

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(fileName.Length);
            foreach (var ch in fileName.Trim())
            {
                builder.Append(invalid.Contains(ch) ? '_' : ch);
            }

            return builder.ToString();
        }

        private async Task SafeDeleteFromStorageAsync(string objectKey)
        {
            try
            {
                await _objectStorage.DeleteAsync(
                    _storageOptions.Bucket,
                    objectKey,
                    HttpContext.RequestAborted
                );
            }
            catch
            {
                // Ignore delete errors
            }
        }

        #endregion
    }
}
