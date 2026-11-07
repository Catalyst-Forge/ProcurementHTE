using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;
using ProcurementHTE.Web.Extensions;
using ProcurementHTE.Web.TagHelpers;

namespace ProcurementHTE.Tests.Services;

public partial class ListSortTests
{
    private sealed record FixedSort(ListSort? Current) : IListSortProvider;

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Vendor NewVendor(string name) => new()
    {
        VendorCode = name, VendorName = name, NPWP = "-", Address = "-", City = "-", Province = "-", Email = "v@test",
    };

    [Theory]
    [InlineData(false, "Alpha", "Beta")]
    [InlineData(true, "Zulu", "Beta")]
    public async Task Vendors_sort_across_every_page(bool descending, string firstOnPageOne, string firstOnPageTwo)
    {
        await using var db = NewDb();
        db.Vendors.AddRange(NewVendor("Zulu"), NewVendor("Alpha"), NewVendor("Beta"));
        await db.SaveChangesAsync();
        var repository = new VendorRepository(db, new FixedSort(new ListSort("vendor", descending)));
        var fields = new HashSet<string>();

        var pageOne = await repository.GetPagedAsync(1, 1, null, fields);
        var pageTwo = await repository.GetPagedAsync(2, 1, null, fields);

        Assert.Equal(firstOnPageOne, pageOne.Items.Single().VendorName);
        Assert.Equal(firstOnPageTwo, pageTwo.Items.Single().VendorName);
    }

    [Fact]
    public async Task Unknown_sort_key_keeps_the_default_order()
    {
        await using var db = NewDb();
        var older = NewVendor("Older");
        older.CreatedAt = DateTime.Now.AddDays(-1);
        db.Vendors.AddRange(older, NewVendor("Newer"));
        await db.SaveChangesAsync();
        var repository = new VendorRepository(db, new FixedSort(new ListSort("NPWP; DROP TABLE", false)));

        var page = await repository.GetPagedAsync(1, 1, null, new HashSet<string>());

        Assert.Equal("Newer", page.Items.Single().VendorName);
    }

    [Fact]
    public async Task Ldp_sorts_mapped_rows_before_paging()
    {
        await using var db = NewDb();
        db.Procurements.AddRange(
            new Procurement { ProcNum = "1", Wonum = "WO-A" },
            new Procurement { ProcNum = "2", Wonum = "WO-C" },
            new Procurement { ProcNum = "3", Wonum = "WO-B" }
        );
        await db.SaveChangesAsync();
        var repository = new LdpRepository(db, new FixedSort(new ListSort("NoWo", true)));

        var (items, total) = await repository.GetAllAsync(2, 1);

        Assert.Equal(3, total);
        Assert.Equal("WO-B", items.Single().NoWo);
    }

    [Fact]
    public void Query_helpers_keep_filters_and_restart_paging()
    {
        var request = new DefaultHttpContext().Request;
        request.QueryString = new QueryString("?search=krakatau&page=4&partial=true");

        var sorted = request.WithSort("vendor", descending: true);

        Assert.Contains("search=krakatau", sorted);
        Assert.Contains("sort=vendor", sorted);
        Assert.Contains("dir=desc", sorted);
        Assert.Contains("page=1", sorted);
        Assert.DoesNotContain("partial", sorted);
        Assert.Contains("page=7", request.WithPage(7));
    }

    [Theory]
    [InlineData("", null, "dir=asc", "bi-chevron-expand")]
    [InlineData("?sort=vendor", "ascending", "dir=desc", "bi-caret-up-fill")]
    [InlineData("?sort=vendor&dir=desc", "descending", "dir=asc", "bi-caret-down-fill")]
    [InlineData("?sort=city", null, "dir=asc", "bi-chevron-expand")]
    public async Task Header_shows_state_and_links_to_the_next_direction(
        string query,
        string? ariaSort,
        string nextDirection,
        string icon
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString(query);
        var helper = new SortableHeaderTagHelper
        {
            Key = "vendor",
            ViewContext = new ViewContext { HttpContext = httpContext },
        };
        var output = new TagHelperOutput(
            "th",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetContent("Vendor"))
        );

        await helper.ProcessAsync(new TagHelperContext([], new Dictionary<object, object>(), "id"), output);

        var html = output.Content.GetContent();
        Assert.Equal(ariaSort, output.Attributes["aria-sort"]?.Value?.ToString());
        Assert.Contains(nextDirection, html);
        Assert.Contains(icon, html);
        Assert.Contains("Vendor", html);
    }

    [GeneratedRegex("sort-key=\"([A-Za-z0-9]+)\"")]
    private static partial Regex SortKey();

    [Theory]
    [InlineData("Vendors/Index.cshtml", "Vendors")]
    [InlineData("DocumentType/Index.cshtml", "DocumentTypes")]
    [InlineData("JobType/Index.cshtml", "JobTypes")]
    [InlineData("PurchaseRequisitions/Index.cshtml", "PurchaseRequisitions")]
    [InlineData("Procurements/Partials/_List.cshtml", "Procurements")]
    [InlineData("ApPoPickup/Partials/_Table.cshtml", "Procurements")]
    [InlineData("ApInvoicePickup/Partials/_Table.cshtml", "Procurements")]
    [InlineData("ArPickup/Index.cshtml", "Procurements")]
    [InlineData("Accrual/Partials/_AccrualTable.cshtml", "Procurements")]
    [InlineData("ProcurementTracking/_PendingApprovalsTable.cshtml", "PendingApprovals")]
    [InlineData("UserManagement/Partials/_UserTable.cshtml", "Users")]
    public void Every_header_key_has_a_column(string view, string list)
    {
        var map = typeof(ListSortColumns).GetField(list)!.GetValue(null)!;
        var keys = ((System.Collections.IEnumerable)map.GetType().GetProperty("Keys")!.GetValue(map)!)
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var used = SortKey().Matches(File.ReadAllText(ViewPath(view))).Select(m => m.Groups[1].Value).ToList();

        Assert.NotEmpty(used);
        Assert.All(used, key => Assert.Contains(key, keys));
    }

    [Fact]
    public void Every_ldp_header_key_is_a_sortable_property()
    {
        var used = SortKey().Matches(File.ReadAllText(ViewPath("Ldp/_LdpTable.cshtml"))).Select(m => m.Groups[1].Value).ToList();

        Assert.Equal(44, used.Count);
        Assert.All(used, key => Assert.NotNull(LdpRepository.FindSortProperty(new ListSort(key, false))));
    }

    private static string ViewPath(string view)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "ProcurementHTE.Web", "Views", view);
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException(view);
    }
}
