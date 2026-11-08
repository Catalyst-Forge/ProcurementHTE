using System.Security.Claims;

namespace ProcurementHTE.Web.Navigation;

public sealed record SidebarItem(
    string Label,
    string Icon,
    string Controller,
    string DataPage,
    string[]? Roles = null
)
{
    public string Action { get; init; } = "Index";

    /// <summary>Controllers that mark this item active; defaults to <see cref="Controller"/>.</summary>
    public string[]? ActiveControllers { get; init; }

    /// <summary>Actions that mark this item active; null matches any action.</summary>
    public string[]? ActiveActions { get; init; }

    public bool HxBoost { get; init; } = true;

    public bool ShowPendingApprovalBadge { get; init; }

    public IReadOnlyList<string> MatchControllers => ActiveControllers ?? [Controller];

    public bool IsVisibleTo(ClaimsPrincipal user) => Roles == null || Roles.Any(user.IsInRole);
}

public sealed record SidebarSection(string? Label, IReadOnlyList<SidebarItem> Items);

/// <summary>
/// Single source of truth for the sidebar: which menus exist, how they are
/// grouped, and which roles see them. Items without roles are visible to every
/// signed-in user.
/// </summary>
public static class SidebarMenu
{
    private const string Admin = "Admin";

    private static readonly string[] DashboardControllers =
    [
        "Dashboard",
        "AdminDashboard",
        "OperationDashboard",
        "ManagerTransportDashboard",
        "AnalystHteDashboard",
        "AssistantManagerHteDashboard",
        "VicePresidentDashboard",
    ];

    public static readonly IReadOnlyList<SidebarSection> Sections =
    [
        new(
            null,
            [
                new("Dashboard", "bi-grid", "Dashboard", "Dashboard")
                {
                    ActiveControllers = DashboardControllers,
                    HxBoost = false,
                },
            ]
        ),
        new(
            "Pengadaan",
            [
                new("Procurements", "bi-briefcase", "Procurements", "Index Procurements"),
                new("PR (Purchase Requisition)", "bi-journal-text", "PurchaseRequisitions", "Index PR Service"),
                new("Laporan Data Procurement", "bi-table", "Ldp", "LDP Data"),
                new("Arsip LDP", "bi-archive", "LdpArchive", "Arsip LDP"),
            ]
        ),
        new(
            "Persetujuan & Pelacakan",
            [
                new(
                    "Pending Approvals",
                    "bi-hourglass-split",
                    "ProcurementTracking",
                    "Pending Approvals"
                )
                {
                    Action = "PendingApprovals",
                    ActiveActions = ["PendingApprovals"],
                    ShowPendingApprovalBadge = true,
                },
                new("Procurement Tracking", "bi-graph-up-arrow", "ProcurementTracking", "Procurement Tracking")
                {
                    ActiveActions = ["Index", "Search", "Details", "TrackingResult"],
                },
            ]
        ),
        new(
            "Keuangan",
            [
                new("Account Payable Purchase Order", "bi-clipboard-check", "ApPoPickup", "AP-PO Pickup"),
                new("Account Payable Invoice", "bi-receipt", "ApInvoicePickup", "AP-Invoice Pickup"),
                new("Account Receivable", "bi-box-arrow-in-down", "ArPickup", "AR Pickup"),
                new("Data Accrual", "bi-calculator", "Accrual", "Data Accrual"),
            ]
        ),
        new(
            "Master Data",
            [
                new("Vendors", "bi-shop", "Vendors", "Vendors", [Admin]),
                new("Procurement Types", "bi-tags", "JobType", "Index Procurement Types", [Admin]),
                new("Document Types", "bi-file-earmark-text", "DocumentType", "Index Document Types", [Admin]),
            ]
        ),
        new(
            "Rules & Mapping",
            [
                new("Job Type Documents", "bi-diagram-3", "JobTypeDocument", "Index Job Type Documents", [Admin]),
                new("Document Approvals", "bi-check2-square", "DocumentApprovals", "Index Document Approvals", [Admin]),
                new("Document Approval Rules", "bi-sliders", "DocumentApprovalRules", "Index Document Approval Rules", [Admin]),
            ]
        ),
        new(
            "Admin",
            [
                new("User Management", "bi-people", "UserManagement", "User Management", [Admin]),
                new("Data Terhapus", "bi-trash3", "DeletedRecords", "Data Terhapus", [Admin]),
            ]
        ),
    ];

    /// <summary>The sections this user can see, with empty sections dropped.</summary>
    public static IReadOnlyList<SidebarSection> For(ClaimsPrincipal user) =>
        Sections
            .Select(section => section with
            {
                Items = section.Items.Where(item => item.IsVisibleTo(user)).ToList(),
            })
            .Where(section => section.Items.Count > 0)
            .ToList();
}
