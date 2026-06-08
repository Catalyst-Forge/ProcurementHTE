using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private async Task RepopulateEditViewModel(ProcurementEditViewModel viewModel)
    {
        var (jobTypes, statuses) =
            await _queryService.GetRelatedEntitiesForProcurementAsync();
        viewModel.JobTypes = jobTypes;
        viewModel.Statuses = statuses;

        await PopulateEditUserSelectListsAsync(viewModel);

        ViewBag.SelectedJobTypeName =
            jobTypes.FirstOrDefault(jobType => jobType.JobTypeId == viewModel.JobTypeId)
                ?.TypeName
            ?? "Other";
    }

    private async Task PopulateEditUserSelectListsAsync(ProcurementEditViewModel viewModel)
    {
        viewModel.PicOpsUsers = await BuildUserSelectListAsync(
            "Operator",
            viewModel.PicOpsUserId
        );
        viewModel.AnalystUsers = await BuildUserSelectListAsync(
            "Analyst HTE & LTS",
            viewModel.AnalystHteUserId
        );
        viewModel.AssistantManagerUsers = await BuildUserSelectListAsync(
            "Assistant Manager HTE",
            viewModel.AssistantManagerUserId
        );
        viewModel.ManagerUsers = await BuildUserSelectListAsync(
            "Manager Transport & Logistic",
            viewModel.ManagerUserId
        );
    }
}
