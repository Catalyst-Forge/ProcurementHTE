using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels;

public class ProcurementCreateViewModel
{
    public Procurement Procurement { get; set; } = new();
    public List<ProcDetail> Details { get; set; } = [];
    public List<ProcOffer> Offers { get; set; } = [];

    public IEnumerable<SelectListItem> PicOpsUsers { get; set; } = [];
    public IEnumerable<SelectListItem> AnalystUsers { get; set; } = [];
    public IEnumerable<SelectListItem> AssistantManagerUsers { get; set; } = [];
    public IEnumerable<SelectListItem> ManagerUsers { get; set; } = [];
}

public class ProcurementEditViewModel
{
    [Required]
    public string ProcurementId { get; set; } = default!;

    [DisplayName("Procurement No.")]
    public string ProcNum { get; set; } = default!;

    public string? JobTypeId { get; set; }

    [DisplayName("Job Type")]
    public string? JobType { get; set; }

    [DisplayName("Contract Type")]
    public ContractType ContractType { get; set; }

    [DisplayName("Job Name")]
    public string? JobName { get; set; }

    [DisplayName("SPK Number")]
    public string? SpkNumber { get; set; }

    [DisplayName("WO Number")]
    [MaxLength(100)]
    public string? Wonum { get; set; }

    [DisplayName("Start Date")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime StartDate { get; set; }

    [DisplayName("End Date")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime EndDate { get; set; }

    [DisplayName("Project Region")]
    public ProjectRegion ProjectRegion { get; set; }

    [DisplayName("Potential Accrual Date")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    public DateTime? PotentialAccrualDate { get; set; }

    [DisplayName("SPMP Number")]
    public string? SpmpNumber { get; set; }

    [DisplayName("Memo Number")]
    public string? MemoNumber { get; set; }

    [DisplayName("OE Number")]
    public string? OeNumber { get; set; }

    [DisplayName("RA Number")]
    public string? RaNumber { get; set; }

    [DisplayName("Project Code")]
    public string? ProjectCode { get; set; }

    [DisplayName("LTC Name")]
    public string? LtcName { get; set; }

    [DisplayName("Note")]
    [MaxLength(1000)]
    public string? Note { get; set; }

    public int StatusId { get; set; }

    [DisplayName("PIC Operations User Id")]
    public string? PicOpsUserId { get; set; }

    [DisplayName("Analyst HTE User Id")]
    public string? AnalystHteUserId { get; set; }

    [DisplayName("Assistant Manager  User Id")]
    public string? AssistantManagerUserId { get; set; }

    [DisplayName("Manager User Id")]
    public string? ManagerUserId { get; set; }

    public List<ProcDetail> Details { get; set; } = [];
    public List<ProcOffer> Offers { get; set; } = [];

    public IEnumerable<JobTypes>? JobTypes { get; set; }
    public IEnumerable<Status>? Statuses { get; set; }

    public IEnumerable<SelectListItem> PicOpsUsers { get; set; } = [];
    public IEnumerable<SelectListItem> AnalystUsers { get; set; } = [];
    public IEnumerable<SelectListItem> AssistantManagerUsers { get; set; } = [];
    public IEnumerable<SelectListItem> ManagerUsers { get; set; } = [];
}
