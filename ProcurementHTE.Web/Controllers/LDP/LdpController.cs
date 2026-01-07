using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.LDP
{
    [Authorize]
    public class LdpController : Controller
    {
        private readonly ILdpService _ldpService;
        private readonly ILogger<LdpController> _logger;

        public LdpController(ILdpService ldpService, ILogger<LdpController> logger)
        {
            _ldpService = ldpService ?? throw new ArgumentNullException(nameof(ldpService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 25,
            string? search = null,
            CancellationToken ct = default
        )
        {
            try
            {
                var (items, totalCount) = await _ldpService.GetAllAsync(page, pageSize, search, ct);

                var viewModel = new LdpIndexViewModel
                {
                    Items = items,
                    Total = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Search = search,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading LDP data");
                TempData["Error"] = "Terjadi kesalahan saat memuat data LDP.";
                return View(new LdpIndexViewModel());
            }
        }
    }
}
