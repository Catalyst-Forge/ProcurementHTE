using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;

namespace ProcurementHTE.Tests.Services;

public class ListFilterTests
{
    private sealed record Row(string? Name, DateTime? Date, bool Active, decimal? Amount, ProcurementStatus Status);

    private static readonly IReadOnlyDictionary<string, Expression<Func<Row, object?>>> Columns =
        new Dictionary<string, Expression<Func<Row, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = r => r.Name,
            ["date"] = r => r.Date,
            ["active"] = r => r.Active,
            ["amount"] = r => r.Amount,
            ["status"] = r => r.Status,
        };

    private static readonly Row[] Rows =
    [
        new("Angkutan BBM", new DateTime(2026, 11, 9, 13, 30, 0), true, 13_000_000m, ProcurementStatus.WaitingApprovalAnalyst),
        new("Sewa crane", new DateTime(2026, 10, 31), false, 9_000_000m, ProcurementStatus.DonePO),
        new(null, null, true, null, ProcurementStatus.OnCreateDP3),
    ];

    private static string[] Find(string key, string value) =>
        Rows.AsQueryable()
            .FilterBy(new Dictionary<string, string> { [key] = value }, Columns)
            .Select(r => r.Name ?? "(none)")
            .ToArray();

    [Fact]
    public void Text_column_keeps_rows_that_contain_the_value() =>
        Assert.Equal(["Angkutan BBM"], Find("name", "BBM"));

    [Theory]
    [InlineData("09 Nov 2026", new[] { "Angkutan BBM" })]
    [InlineData("09/11/2026", new[] { "Angkutan BBM" })]
    [InlineData("2026-11-09", new[] { "Angkutan BBM" })]
    [InlineData("Nov 2026", new[] { "Angkutan BBM" })]
    [InlineData("10/2026", new[] { "Sewa crane" })]
    [InlineData("Okt 2026", new[] { "Sewa crane" })]
    [InlineData("2026", new[] { "Angkutan BBM", "Sewa crane" })]
    [InlineData("kemarin", new string[0])]
    public void Date_column_takes_a_day_a_month_or_a_year(string typed, string[] expected) =>
        Assert.Equal(expected, Find("date", typed));

    [Theory]
    [InlineData("Aktif", new[] { "Angkutan BBM", "(none)" })]
    [InlineData("Nonaktif", new[] { "Sewa crane" })]
    [InlineData("-", new[] { "Sewa crane" })]
    [InlineData("mungkin", new string[0])]
    public void Yes_no_column_reads_aktif_and_nonaktif(string typed, string[] expected) =>
        Assert.Equal(expected, Find("active", typed));

    [Theory]
    [InlineData("13.000.000")]
    [InlineData("13000000")]
    [InlineData("Rp 13.000.000")]
    public void Amount_column_matches_the_amount_typed(string typed) =>
        Assert.Equal(["Angkutan BBM"], Find("amount", typed));

    [Fact]
    public void Status_column_matches_the_name_or_its_description() =>
        Assert.Equal(["Sewa crane"], Find("status", "DonePO"));

    [Fact]
    public void Unknown_columns_and_blank_values_change_nothing()
    {
        var filters = new Dictionary<string, string> { ["nope"] = "x", ["name"] = "  " };
        Assert.Equal(3, Rows.AsQueryable().FilterBy(filters, Columns).Count());
    }
}

public class ListFilterSqlServerTests
{
    // Constitution II: InMemory does not prove a query translates, so print it with the SQL Server provider.
    private static AppDbContext SqlServer() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=translate-only;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    private static string Sql<T>(IQueryable<T> query, IReadOnlyDictionary<string, Expression<Func<T, object?>>> columns)
    {
        // Every column of the list, each with a value of its own type.
        var filters = columns.ToDictionary(
            c => c.Key,
            c => (Nullable.GetUnderlyingType(Body(c.Value).Type) ?? Body(c.Value).Type) switch
            {
                var t when t == typeof(DateTime) => "Nov 2026",
                var t when t == typeof(bool) => "Aktif",
                var t when t == typeof(decimal) || t == typeof(int) => "13.000.000",
                var t when t.IsEnum => "a",
                _ => "abc",
            });
        return query.FilterBy(filters, columns).ToQueryString();
    }

    private static Expression Body(LambdaExpression column) =>
        column.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert ? convert.Operand : column.Body;

    [Fact]
    public void Every_list_column_search_translates_for_sql_server()
    {
        using var db = SqlServer();

        Assert.Contains("LIKE", Sql(db.Vendors, ListSortColumns.Vendors));
        Assert.Contains("LIKE", Sql(db.DocumentTypes, ListSortColumns.DocumentTypes));
        Assert.Contains("LIKE", Sql(db.JobTypes, ListSortColumns.JobTypes));
        Assert.Contains("[LastLoginAt] >=", Sql(db.Users, ListSortColumns.Users));
        Assert.Contains("[ApprovalTokenGeneratedAt] <", Sql(db.Procurements, ListSortColumns.PendingApprovals));
        Assert.Contains("[StartDate] >=", Sql(db.Procurements, ListSortColumns.Procurements));
    }
}
