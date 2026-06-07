using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private async Task<ProcurementCreateViewModel> BuildCreateViewModelAsync(
        string? jobTypeId,
        ProcurementCategory? category
    )
    {
        var (jobTypes, _) = await _procurementService.GetRelatedEntitiesForProcurementAsync();
        var selectedJobType =
            jobTypes.FirstOrDefault(jobType => jobType.JobTypeId == jobTypeId)
            ?? jobTypes.FirstOrDefault();

        var viewModel = new ProcurementCreateViewModel
        {
            Procurement = new Procurement
            {
                JobTypeId = selectedJobType?.JobTypeId!,
                ProcurementCategory = category ?? ProcurementCategory.Barang,
            },
            Details = [],
            Offers = [],
            JobTypes = jobTypes,
            SelectedJobTypeName = selectedJobType?.TypeName ?? "Other",
        };

        await PopulateCreateUserSelectListsAsync(viewModel);
        ViewBag.UnitTypes = await _unitTypeRepository.GetActiveAsync();
        ViewBag.SelectedJobTypeName = selectedJobType?.TypeName ?? "Other";

        return viewModel;
    }

    private async Task RepopulateCreateViewModel(ProcurementCreateViewModel createViewModel)
    {
        ArgumentNullException.ThrowIfNull(createViewModel);
        createViewModel.Procurement ??= new Procurement();

        await PopulateCreateUserSelectListsAsync(createViewModel);

        var (jobTypes, _) = await _procurementService.GetRelatedEntitiesForProcurementAsync();
        createViewModel.JobTypes = jobTypes;

        if (string.IsNullOrWhiteSpace(createViewModel.Procurement.JobTypeId))
            createViewModel.Procurement.JobTypeId = jobTypes.FirstOrDefault()?.JobTypeId!;

        createViewModel.SelectedJobTypeName =
            jobTypes.FirstOrDefault(jobType => jobType.JobTypeId == createViewModel.Procurement.JobTypeId)
                ?.TypeName
            ?? "Other";

        ViewBag.UnitTypes = await _unitTypeRepository.GetActiveAsync();
        ViewBag.SelectedJobTypeName = createViewModel.SelectedJobTypeName;
    }

    private async Task PopulateCreateUserSelectListsAsync(ProcurementCreateViewModel viewModel)
    {
        viewModel.PicOpsUsers = await BuildUserSelectListAsync(
            "Operator",
            viewModel.Procurement.PicOpsUserId
        );
        viewModel.AnalystUsers = await BuildUserSelectListAsync(
            "Analyst HTE & LTS",
            viewModel.Procurement.AnalystHteUserId
        );
        viewModel.AssistantManagerUsers = await BuildUserSelectListAsync(
            "Assistant Manager HTE",
            viewModel.Procurement.AssistantManagerUserId
        );
        viewModel.ManagerUsers = await BuildUserSelectListAsync(
            "Manager Transport & Logistic",
            viewModel.Procurement.ManagerUserId
        );
    }
}
