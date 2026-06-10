using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private void ValidateSelectedVendorIds(
        ProfitLossInputViewModel viewModel,
        string errorMessage
    )
    {
        if (viewModel.SelectedVendorIds == null || viewModel.SelectedVendorIds.Count == 0)
            ModelState.AddModelError(nameof(viewModel.SelectedVendorIds), errorMessage);
    }

    private static List<string> GetDistinctSelectedVendorIds(ProfitLossInputViewModel viewModel)
    {
        return (viewModel.SelectedVendorIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ValidateProfitLossVendorOffers(ProfitLossInputViewModel viewModel)
    {
        if (viewModel.Vendors == null)
            return;

        foreach (var vendor in viewModel.Vendors)
        {
            if (string.IsNullOrWhiteSpace(vendor.VendorId))
            {
                ModelState.AddModelError(nameof(viewModel.Vendors), "Vendor ID tidak boleh kosong");
                continue;
            }

            if (vendor.Items == null || vendor.Items.Count == 0)
                ModelState.AddModelError(nameof(viewModel.Vendors), "Vendor harus memiliki minimal 1 item penawaran");
        }
    }
}
