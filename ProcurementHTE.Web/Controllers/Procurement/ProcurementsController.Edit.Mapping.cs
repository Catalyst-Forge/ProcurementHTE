using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Models.ViewModels;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private static Procurement BuildProcurementForUpdate(
        ProcurementEditViewModel editViewModel,
        Procurement existingProcurement
    )
    {
        return new Procurement
        {
            ProcurementId = editViewModel.ProcurementId,
            ProcNum = editViewModel.ProcNum,
            JobTypeId = editViewModel.JobTypeId!,
            ContractType = editViewModel.ContractType,
            JobName = editViewModel.JobName!,
            DocumentDate = editViewModel.DocumentDate,
            StartDate = editViewModel.StartDate,
            EndDate = editViewModel.EndDate,
            ProjectRegion = editViewModel.ProjectRegion,
            PotentialAccrualDate = editViewModel.PotentialAccrualDate,
            SpkNumber = editViewModel.SpkNumber,
            SpmpNumber = ProcurementReferenceNumberFormatter.AppendSuffixIfNeeded(editViewModel.SpmpNumber),
            MemoNumber = editViewModel.MemoNumber,
            OeNumber = ProcurementReferenceNumberFormatter.AppendSuffixIfNeeded(editViewModel.OeNumber),
            RaNumber = editViewModel.RaNumber,
            NoRig = editViewModel.NoRig,
            NoHte = editViewModel.NoHte,
            ProjectCode = editViewModel.ProjectCode,
            Wonum = editViewModel.Wonum,
            LtcName = editViewModel.LtcName,
            Note = editViewModel.Note,
            ProcurementCategory = editViewModel.ProcurementCategory,
            PicOpsUserId = existingProcurement.PicOpsUserId,
            AnalystHteUserId = editViewModel.AnalystHteUserId!,
            AssistantManagerUserId = editViewModel.AssistantManagerUserId!,
            ManagerUserId = editViewModel.ManagerUserId!,
            AnalystHtePjs = editViewModel.AnalystHtePjs,
            AssistantManagerPjs = editViewModel.AssistantManagerPjs,
            ManagerPjs = editViewModel.ManagerPjs,
            VicePresidentPjs = editViewModel.VicePresidentPjs,
            OperationDirectorPjs = editViewModel.OperationDirectorPjs,
            PresidentDirectorPjs = editViewModel.PresidentDirectorPjs,
            StatusId = editViewModel.StatusId,
        };
    }
}
