using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Tests.Services;

public class LdpProgressTests
{
    private static LdpRecapDto Complete() => new()
    {
        ProcurementId = "job",
        NoSpmp = "4841",
        TextPekerjaan = "Angkutan BBM",
        NamaVendor = "PT Kurnia Jaya",
        NoPr = "PR-100",
        NoPo = "PO-1",
        SANo = "SA-1",
        SP3No = "SP3-1",
        TglSubmitLoket = new DateTime(2026, 11, 9),
    };

    [Fact]
    public void A_job_with_every_column_filled_is_done() =>
        Assert.Equal(LdpProgress.Done, Complete().Progress);

    [Theory]
    [InlineData(nameof(LdpRecapDto.NoSpmp))]
    [InlineData(nameof(LdpRecapDto.NamaVendor))]
    [InlineData(nameof(LdpRecapDto.NoPo))]
    [InlineData(nameof(LdpRecapDto.SANo))]
    [InlineData(nameof(LdpRecapDto.SP3No))]
    public void A_job_missing_one_column_is_on_progress(string column)
    {
        var row = Complete();
        typeof(LdpRecapDto).GetProperty(column)!.SetValue(row, null);

        Assert.Equal(LdpProgress.OnProgress, row.Progress);
    }

    [Fact]
    public void A_dash_counts_as_empty()
    {
        var row = Complete();
        row.SP3No = "-";

        Assert.Equal(LdpProgress.OnProgress, row.Progress);
    }

    [Fact]
    public void A_job_waits_for_submit_loket_but_an_archived_row_cannot_have_one()
    {
        var job = Complete();
        job.TglSubmitLoket = null;
        var archived = Complete();
        archived.TglSubmitLoket = null;
        archived.ArchiveRowId = "row";

        Assert.Equal(LdpProgress.OnProgress, job.Progress);
        Assert.Equal(LdpProgress.Done, archived.Progress);
    }

    [Fact]
    public void A_cancelled_job_is_cancel_whatever_is_filled()
    {
        var row = Complete();
        row.IsCancelled = true;

        Assert.Equal(LdpProgress.Cancel, row.Progress);
    }

    [Fact]
    public void An_archive_list_row_follows_the_same_rule()
    {
        var done = new LdpArchiveListItem
        {
            Id = "a", NoSpmp = "4841", JobName = "Angkutan", VendorName = "PT A",
            NoPr = "PR-1", NoPo = "PO-1", NoSa = "SA-1", NoSp3 = "SP3-1",
        };
        var open = new LdpArchiveListItem { Id = "b", JobName = "Angkutan", VendorName = "PT A", NoPr = "PR-1" };

        Assert.Equal(LdpProgress.Done, done.Progress);
        Assert.Equal(LdpProgress.OnProgress, open.Progress);
    }
}
