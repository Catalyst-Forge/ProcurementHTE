using ProcurementHTE.Core.Enums;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private async Task ValidateEditViewModelAsync(
        ProcurementEditViewModel editViewModel,
        string? submitAction
    )
    {
        if (string.IsNullOrWhiteSpace(editViewModel.JobTypeId))
            ModelState.AddModelError(nameof(editViewModel.JobTypeId), "Job type wajib dipilih");

        if (editViewModel.ContractType == 0)
            ModelState.AddModelError(nameof(editViewModel.ContractType), "Contract type wajib dipilih");

        if (editViewModel.ProcurementCategory == 0)
            ModelState.AddModelError(nameof(editViewModel.ProcurementCategory), "Jenis pengadaan wajib dipilih");

        if (string.IsNullOrWhiteSpace(editViewModel.JobName))
            ModelState.AddModelError(nameof(editViewModel.JobName), "Nama pekerjaan wajib diisi");

        ValidateEditDates(editViewModel);
        ValidateEditRaNumber(editViewModel);
        ValidateEditApprovers(editViewModel);

        var status = await _procurementService.GetStatusByNameAsync(submitAction ?? "Created");
        if (status == null)
            ModelState.AddModelError("", $"Status '{submitAction}' tidak ditemukan.");
        else
            editViewModel.StatusId = status.StatusId;
    }

    private void ValidateEditDates(ProcurementEditViewModel editViewModel)
    {
        if (editViewModel.StartDate == default)
            ModelState.AddModelError(nameof(editViewModel.StartDate), "Tanggal mulai wajib diisi");

        if (editViewModel.EndDate == default)
            ModelState.AddModelError(nameof(editViewModel.EndDate), "Tanggal selesai wajib diisi");

        if (
            editViewModel.StartDate != default
            && editViewModel.EndDate != default
            && editViewModel.EndDate < editViewModel.StartDate
        )
        {
            ModelState.AddModelError(nameof(editViewModel.EndDate), "Tanggal selesai harus setelah tanggal mulai");
        }
    }

    private void ValidateEditRaNumber(ProcurementEditViewModel editViewModel)
    {
        if (
            editViewModel.ProcurementCategory == ProcurementCategory.Jasa
            && string.IsNullOrWhiteSpace(editViewModel.RaNumber)
        )
        {
            ModelState.AddModelError(
                nameof(editViewModel.RaNumber),
                "RA Number wajib diisi untuk jenis pengadaan jasa"
            );
        }
    }

    private void ValidateEditApprovers(ProcurementEditViewModel editViewModel)
    {
        if (string.IsNullOrWhiteSpace(editViewModel.AnalystHteUserId))
            ModelState.AddModelError(nameof(editViewModel.AnalystHteUserId), "Analyst HTE & LTS wajib dipilih");

        if (string.IsNullOrWhiteSpace(editViewModel.AssistantManagerUserId))
            ModelState.AddModelError(nameof(editViewModel.AssistantManagerUserId), "Assistant Manager HTE wajib dipilih");

        if (string.IsNullOrWhiteSpace(editViewModel.ManagerUserId))
            ModelState.AddModelError(nameof(editViewModel.ManagerUserId), "Manager Transport & Logistic wajib dipilih");
    }
}
