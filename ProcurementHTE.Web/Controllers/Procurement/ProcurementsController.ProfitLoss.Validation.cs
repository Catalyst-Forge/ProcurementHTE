using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private void RemoveProfitLossItemProcOfferValidation()
    {
        foreach (var key in Request.Form["Items.Index"].ToArray())
            ModelState.Remove($"Items[{key}].ProcOfferId");
    }

    private void ValidateProfitLossFields(
        ProfitLossInputViewModel viewModel,
        bool requireProfitLossId
    )
    {
        if (
            requireProfitLossId
            && viewModel is ProfitLossEditViewModel editModel
            && string.IsNullOrWhiteSpace(editModel.ProfitLossId)
        )
        {
            ModelState.AddModelError(nameof(editModel.ProfitLossId), "Profit Loss ID tidak valid");
        }

        if (string.IsNullOrWhiteSpace(viewModel.ProcurementId))
            ModelState.AddModelError(nameof(viewModel.ProcurementId), "Procurement ID tidak valid");

        ValidateProfitLossItems(viewModel, requireProfitLossId);
        ValidateProfitLossAmounts(viewModel);
    }

    private void ValidateProfitLossItems(ProfitLossInputViewModel viewModel, bool isEdit)
    {
        if (viewModel.Items == null || viewModel.Items.Count == 0)
        {
            ModelState.AddModelError(
                nameof(viewModel.Items),
                isEdit
                    ? "Minimal harus ada 1 item tarif"
                    : "Minimal harus ada 1 item tarif untuk membuat Profit & Loss"
            );
            return;
        }

        for (int i = 0; i < viewModel.Items.Count; i++)
        {
            var item = viewModel.Items[i];
            if (string.IsNullOrWhiteSpace(item.ProcOfferId))
                ModelState.AddModelError($"Items[{i}].ProcOfferId", "Proc Offer ID tidak boleh kosong");

            if (item.Quantity <= 0)
                ModelState.AddModelError($"Items[{i}].Quantity", "Quantity harus lebih dari 0");

            if (item.TarifAwal < 0m)
                ModelState.AddModelError($"Items[{i}].TarifAwal", "Tarif Awal tidak boleh negatif");

            if (item.TarifAdd < 0m)
                ModelState.AddModelError($"Items[{i}].TarifAdd", "Tarif Add tidak boleh negatif");

            if (item.OperatorCost < 0m)
                ModelState.AddModelError($"Items[{i}].OperatorCost", "Operator Cost tidak boleh negatif");
        }
    }

    private void ValidateProfitLossAmounts(ProfitLossInputViewModel viewModel)
    {
        if (viewModel.AccrualAmount.HasValue && viewModel.AccrualAmount.Value < 0)
            ModelState.AddModelError(nameof(viewModel.AccrualAmount), "Accrual Amount tidak boleh negatif");

        if (viewModel.RealizationAmount.HasValue && viewModel.RealizationAmount.Value < 0)
            ModelState.AddModelError(nameof(viewModel.RealizationAmount), "Realization Amount tidak boleh negatif");

        if (viewModel.Distance < 0)
            ModelState.AddModelError(nameof(viewModel.Distance), "Distance tidak boleh negatif");
    }
}
