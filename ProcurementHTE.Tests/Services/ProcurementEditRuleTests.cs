using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Services;

namespace ProcurementHTE.Tests.Services;

public class ProcurementEditRuleTests
{
    private static Procurement Job(string status, ProcurementStatus stage = ProcurementStatus.OnCreateDP3) =>
        new() { Status = new Status { StatusName = status }, ProcurementStatus = stage };

    [Theory]
    [InlineData("Draft", true)]
    [InlineData("Created", true)]
    [InlineData("In Progress", true)]
    [InlineData("Completed", true)]
    [InlineData("Cancelled", false)]
    public void Admin_edits_a_job_at_any_stage_but_cancelled(string status, bool allowed) =>
        Assert.Equal(allowed, ProcurementService.CanEdit(Job(status), isAdmin: true));

    [Theory]
    [InlineData("Draft", true)]
    [InlineData("created", true)]
    [InlineData("Waiting Pickup", false)]
    [InlineData("In Progress", false)]
    [InlineData("Completed", false)]
    [InlineData("Cancelled", false)]
    public void Everyone_else_edits_only_before_publishing(string status, bool allowed) =>
        Assert.Equal(allowed, ProcurementService.CanEdit(Job(status), isAdmin: false));

    [Fact]
    public void A_cancelled_job_is_not_edited_even_while_its_status_row_says_otherwise() =>
        Assert.False(ProcurementService.CanEdit(Job("In Progress", ProcurementStatus.Cancelled), isAdmin: true));
}
