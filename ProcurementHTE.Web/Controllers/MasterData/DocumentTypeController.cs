using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.MasterData
{
    [Authorize]
    public class DocumentTypeController : Controller
    {
        private readonly IDocumentTypeService _documentTypeService;
        private const string ActivePageName = "Index Document Types";

        public DocumentTypeController(IDocumentTypeService documentTypeService)
        {
            _documentTypeService = documentTypeService;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActivePage = ActivePageName;
            base.OnActionExecuting(context);
        }

        // GET: DocumentType
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? fields = null,
            CancellationToken ct = default
        )
        {
            var allowed = new[] { 10, 25, 50, 100 };
            if (!allowed.Contains(pageSize))
                pageSize = 10;

            var selectedFields = (fields ?? "Name, Description")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var documentTypes = await _documentTypeService.GetAllDocumentTypesAsync(
                page,
                pageSize,
                search,
                selectedFields,
                ct
            );
            ViewBag.RouteData = new RouteValueDictionary
            {
                ["ActivePage"] = ActivePageName,
                ["search"] = search,
                ["fields"] = string.Join(',', selectedFields),
                ["pageSize"] = pageSize,
            };
            return View(documentTypes);
        }

        // GET: DocumentType/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DocumentType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description")] DocumentType documentType
        )
        {
            if (!ModelState.IsValid)
            {
                return View(documentType);
            }
            try
            {
                await _documentTypeService.AddDocumentTypeAsync(documentType);
                TempData["SuccessMessage"] = "Document type added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to add document type: " + ex.Message;
                return View(documentType);
            }
        }

        // GET: DocumentType/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id != null)
            {
                var documentType = await _documentTypeService.GetDocumentTypeByIdAsync(id);
                return View(documentType);
            }
            return NotFound();
        }

        // POST: DocumentType/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            [Bind("Id,Name,Description")] DocumentType documentType
        )
        {
            if (id != documentType.DocumentTypeId)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                return View(documentType);
            }
            try
            {
                await _documentTypeService.EditDocumentTypeAsync(documentType, id);
                TempData["SuccessMessage"] = "Document type updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update document type: " + ex.Message;
                return View(documentType);
            }
        }

        // GET: DocumentType/Delete/5
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var documentType = await _documentTypeService.GetDocumentTypeByIdAsync(id);
                if (documentType == null)
                {
                    TempData["ErrorMessage"] = "Document type not found.";
                    return NotFound();
                }
                await _documentTypeService.DeleteDocumentTypeAsync(documentType);
                TempData["SuccessMessage"] = "Document type deleted successfully.";
            }
            catch (DbUpdateException ex)
            {
                // tampilkan pesan SQL aslinya
                var inner = ex.InnerException?.Message ?? ex.Message;
                TempData["ErrorMessage"] = $"DBUpdateException: {inner}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete document type: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
