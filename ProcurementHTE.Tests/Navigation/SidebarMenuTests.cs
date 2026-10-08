using System.Security.Claims;
using ProcurementHTE.Web.Navigation;

namespace ProcurementHTE.Tests.Navigation;

public class SidebarMenuTests
{
    private static readonly string[] EveryoneSees =
    [
        "Dashboard",
        "LDP",
        "Procurement Tracking",
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
    [InlineData("Operation", new[] { "Procurements" })]
    [InlineData("Analyst HTE & LTS", new[] { "Procurements", "Pending Approvals" })]
    [InlineData("Assistant Manager HTE", new[] { "Pending Approvals" })]
    [InlineData("Manager Transport & Logistic", new[] { "Pending Approvals" })]
    [InlineData(
        "AP-PO",
        new[]
        {
            "PR Service",
            "AP-PO Pickup",
            "Vendors",
            "Procurement Types",
            "Document Types",
            "Job Type Documents",
            "Document Approvals",
            "Document Approval Rules",
        }
    )]
    [InlineData("AP-Invoice", new[] { "AP-Invoice Pickup" })]
    [InlineData("AR", new[] { "AR Pickup" })]
    [InlineData("Vice President", new string[0])]
    [InlineData("Dewan Direksi", new string[0])]
    [InlineData("HSE", new string[0])]
    public void For_Role_ShowsSharedMenusPlusItsOwn(string role, string[] roleSpecific)
    {
        var expected = EveryoneSees.Concat(roleSpecific).OrderBy(x => x);

        Assert.Equal(expected, VisibleLabels(role).OrderBy(x => x));
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

        Assert.Contains("AP-Invoice Pickup", labels);
        Assert.Contains("AR Pickup", labels);
        Assert.Equal(labels.Length, labels.Distinct().Count());
    }

    [Fact]
    public void For_DropsGroupsWithNoVisibleItems()
    {
        var sections = SidebarMenu.For(UserWithRoles("HSE"));

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
                "Approval & Pelacakan",
                "Keuangan",
                "Master Data",
                "Rules & Mapping",
                "Admin",
            },
            labels
        );
    }
}
