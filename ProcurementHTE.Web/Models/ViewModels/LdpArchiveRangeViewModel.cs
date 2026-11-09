using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels;

/// <summary>The LDP range form on Arsip LDP, with the range in force and its history.</summary>
public class LdpArchiveRangeViewModel
{
    [DataType(DataType.Date)]
    [Display(Name = "Tanggal awal")]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Tanggal akhir")]
    public DateTime? To { get; set; }

    [BindNever]
    public LdpArchiveRange Current { get; set; } = LdpArchiveRange.Default;

    [BindNever]
    public IReadOnlyList<LdpArchiveRangeChange> History { get; set; } = [];
}
