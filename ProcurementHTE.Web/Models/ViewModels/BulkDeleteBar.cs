namespace ProcurementHTE.Web.Models.ViewModels;

/// <summary>
/// The bar under a list that deletes the ticked rows (Views/Shared/_BulkDeleteBar.cshtml).
/// The row checkboxes join its form through <c>form="@FormId"</c>.
/// </summary>
public sealed record BulkDeleteBar(string FormId, string Action, string ReturnUrl);
