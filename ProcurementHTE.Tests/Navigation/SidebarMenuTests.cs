using System.Security.Claims;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Web.Navigation;

namespace ProcurementHTE.Tests.Navigation;

public class SidebarMenuTests
{
    private static readonly string[] EveryoneSees =
    [
        "Dashboard",
        "Procurements",
        "LDP",
        "Arsip LDP",
        "Pending Approvals",
        "Procurement Tracking",
        "AP Purchase Order",
        "AP Invoice",
        "Data Accrual",
    ];

    private static ClaimsPrincipal UserWithRoles(params string[] roles) =>
        new(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "test"));

    private static string[] VisibleLabels(params string[] roles) =>
        SidebarMenu
            .For(UserWithRoles(roles))
            .SelectMany(section => section.Items)
            .Select(item => item.Label)
            .ToArray();

    [Theory]
    [InlineData("Operation")]
    [InlineData("Analyst HTE & LTS")]
    [InlineData(RoleNames.AsstManager)]
    [InlineData("Manager Transport & Logistic")]
    [InlineData("AP-PO")]
    [InlineData("AP-Invoice")]
    [InlineData("AR")]
    [InlineData("Vice President")]
    [InlineData(RoleNames.Accrual)]
    public void For_EveryRoleButAdmin_SeesTheWorkflowMenusOnly(string role)
    {
        Assert.Equal(EveryoneSees.OrderBy(x => x), VisibleLabels(role).OrderBy(x => x));
    }

    [Fact]
    public void For_Admin_SeesEveryMenu()
    {
        var all = SidebarMenu.Sections.SelectMany(s => s.Items).Select(i => i.Label);

        Assert.Equal(all, VisibleLabels("Admin"));
    }

    [Fact]
    public void For_UserWithSeveralRoles_GetsUnionWithoutDuplicates()
    {
        var labels = VisibleLabels("AP-Invoice", "AR");

        Assert.Contains("AP Invoice", labels);
        Assert.Contains("Data Accrual", labels);
        Assert.DoesNotContain("Account Receivable", labels);
        Assert.Equal(labels.Length, labels.Distinct().Count());
    }

    [Fact]
    public void For_DropsGroupsWithNoVisibleItems()
    {
        var sections = SidebarMenu.For(UserWithRoles("Operation"));

        Assert.All(sections, s => Assert.NotEmpty(s.Items));
        Assert.DoesNotContain(sections, s => s.Label == "Master Data");
        Assert.DoesNotContain(sections, s => s.Label == "Admin");
    }

    [Fact]
    public void Sections_KeepWorkflowOrder()
    {
        var labels = SidebarMenu.Sections.Select(s => s.Label);

        Assert.Equal(
            new string?[]
            {
                null,
                "Pengadaan",
                "Persetujuan & Pelacakan",
                "Keuangan",
                "Master Data",
                "Rules & Mapping",
                "Admin",
            },
            labels
        );
    }
}
