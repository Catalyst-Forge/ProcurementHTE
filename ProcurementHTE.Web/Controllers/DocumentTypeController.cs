using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers
{
    public class DocumentTypeController : Controller
    {
        private readonly IDocumentTypeService _documentTypeService;
        public DocumentTypeController(IDocumentTypeService documentTypeService)
        {
            _documentTypeService = documentTypeService;
        }

        // GET: DocumentType
        public async Task<IActionResult> Index()
        {
            var documentTypes = await _documentTypeService.GetAllDocumentTypesAsync();
            ViewBag.ActivePage = "DocumentType";
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
        public async Task<IActionResult> Create([Bind("Name,Description")] DocumentType documentType)
        {
            if (!ModelState.IsValid) return View(documentType);
            try
            {
                await _documentTypeService.AddDocumentTypeAsync(documentType);
                TempData["SuccessMessage"] = "Document type berhasil ditambahkan.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menambahkan data: " + ex.Message;
                return View(documentType);
            }
        }
        // GET: DocumentType/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }
            var documentType = await _documentTypeService.GetDocumentTypeByIdAsync(id);
            return View(documentType);
        }
        // POST: DocumentType/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] DocumentType documentType)
        {
            if (id != documentType.Id)
            {
                return NotFound();
            }
            if (!ModelState.IsValid) return View(documentType);
            try
            {
                await _documentTypeService.EditDocumentTypeAsync(documentType, id);
                TempData["SuccessMessage"] = "Document type berhasil diupdate.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal mengupdate data: " + ex.Message;
                return View(documentType);
            }
        }
        // GET: DocumentType/Delete/5
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var documentType = await _documentTypeService.GetDocumentTypeByIdAsync(id);
                if (documentType == null)
                {
                    TempData["ErrorMessage"] = "Document type tidak ditemukan.";
                    return NotFound();
                }
                await _documentTypeService.DeleteDocumentTypeAsync(documentType);
                TempData["SuccessMessage"] = "Document type berhasil dihapus.";
            }
            catch (DbUpdateException ex)
            {
                // tampilkan pesan SQL aslinya
                var inner = ex.InnerException?.Message ?? ex.Message;
                TempData["ErrorMessage"] = $"DBUpdateException: {inner}";
                Console.WriteLine("[DEBUG] SQL ERROR: " + inner);
            } catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menghapus data: " + ex.Message;
                Console.WriteLine("[DEBUG] ERROR: " + ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
