using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels;

public class ProcurementCreateViewModel
{
    public Procurement Procurement { get; set; } = new();
    public List<ProcDetail> Details { get; set; } = [];
    public List<ProcOffer> Offers { get; set; } = [];
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

    [DisplayName("Job Type (Other)")]
    public string? JobTypeOther { get; set; }

    [DisplayName("Contract Type")]
    public string? ContractType { get; set; }

    [DisplayName("Job Name")]
    public string? JobName { get; set; }

    [DisplayName("SPK Number")]
    public string? SpkNumber { get; set; }

    [DisplayName("Start Date")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DisplayName("End Date")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [DisplayName("Project Region")]
    public string? ProjectRegion { get; set; }

    [DisplayName("Distance (Km)")]
    public decimal? DistanceKm { get; set; }

    [DisplayName("Accrual Amount")]
    public decimal? AccrualAmount { get; set; }

    [DisplayName("Realization Amount")]
    public decimal? RealizationAmount { get; set; }

    [DisplayName("Potential Accrual Date")]
    [DataType(DataType.Date)]
    public DateTime? PotentialAccrualDate { get; set; }

    [DisplayName("SPMP Number")]
    public string? SpmpNumber { get; set; }

    [DisplayName("Memo Number")]
    public string? MemoNumber { get; set; }

    [DisplayName("OE Number")]
    public string? OeNumber { get; set; }

    [DisplayName("Selected Vendor Name")]
    public string? SelectedVendorName { get; set; }

    [DisplayName("Vendor SPH Number")]
    public string? VendorSphNumber { get; set; }

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

    [DisplayName("Analyst HTE Signer User Id")]
    public string? AnalystHteSignerUserId { get; set; }

    [DisplayName("Assistant Manager Signer User Id")]
    public string? AssistantManagerSignerUserId { get; set; }

    [DisplayName("Manager Signer User Id")]
    public string? ManagerSignerUserId { get; set; }

    public List<ProcDetail> Details { get; set; } = [];
    public List<ProcOffer> Offers { get; set; } = [];

    public IEnumerable<JobTypes>? JobTypes { get; set; }
    public IEnumerable<Status>? Statuses { get; set; }
}
