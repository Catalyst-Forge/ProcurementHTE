using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Models.ViewModels;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    [Authorize(Policy = Permissions.Procurement.Edit)]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var procurement = await _queryService.GetProcurementByIdAsync(id);
        if (procurement == null)
            return NotFound();

        if (!CanUserEditProcurementByStatus(procurement))
        {
            TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit procurement dengan status ini.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            return View(await BuildEditViewModelAsync(procurement));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to load procurement for editing: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<ProcurementEditViewModel> BuildEditViewModelAsync(Procurement procurement)
    {
        var (jobTypes, statuses) = await _queryService.GetRelatedEntitiesForProcurementAsync();
        var viewModel = new ProcurementEditViewModel
        {
            ProcurementId = procurement.ProcurementId,
            ProcNum = procurement.ProcNum ?? string.Empty,
            JobTypeId = procurement.JobTypeId,
            ContractType = procurement.ContractType,
            ProcurementCategory = procurement.ProcurementCategory,
            JobName = procurement.JobName,
            SpkNumber = procurement.SpkNumber,
            DocumentDate = procurement.DocumentDate,
            StartDate = procurement.StartDate,
            EndDate = procurement.EndDate,
            ProjectRegion = procurement.ProjectRegion,
            PotentialAccrualDate = procurement.PotentialAccrualDate,
            SpmpNumber = ProcurementReferenceNumberFormatter.RemoveSuffixIfNeeded(procurement.SpmpNumber),
            MemoNumber = procurement.MemoNumber,
            OeNumber = ProcurementReferenceNumberFormatter.RemoveSuffixIfNeeded(procurement.OeNumber),
            RaNumber = procurement.RaNumber,
            NoRig = procurement.NoRig,
            NoHte = procurement.NoHte,
            ProjectCode = procurement.ProjectCode,
            Wonum = procurement.Wonum,
            LtcName = procurement.LtcName,
            Note = procurement.Note,
            StatusId = procurement.StatusId,
            PicOpsUserId = procurement.PicOpsUserId,
            AnalystHteUserId = procurement.AnalystHteUserId,
            AssistantManagerUserId = procurement.AssistantManagerUserId,
            ManagerUserId = procurement.ManagerUserId,
            AnalystHtePjs = procurement.AnalystHtePjs,
            AssistantManagerPjs = procurement.AssistantManagerPjs,
            ManagerPjs = procurement.ManagerPjs,
            VicePresidentPjs = procurement.VicePresidentPjs,
            OperationDirectorPjs = procurement.OperationDirectorPjs,
            PresidentDirectorPjs = procurement.PresidentDirectorPjs,
            Details = procurement.ProcDetails?.ToList() ?? [],
            Offers = procurement.ProcOffers?.ToList() ?? [],
            JobTypes = jobTypes,
            Statuses = statuses,
        };

        await PopulateEditUserSelectListsAsync(viewModel);
        ViewBag.UnitTypes = await _unitTypeRepository.GetActiveAsync();
        ViewBag.SelectedJobTypeName = procurement.JobType?.TypeName ?? "Other";

        return viewModel;
    }
}
