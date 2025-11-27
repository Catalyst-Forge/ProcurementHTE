using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs;

public class ProcurementDetailDto
{
    [MaxLength(255)]
    public string? ItemName { get; set; }

    public decimal? Quantity { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    [MaxLength(32)]
    public string? DetailKind { get; set; }

    public string? VendorId { get; set; }
}

public class ProcurementOfferDto
{
    [Required]
    public string ItemPenawaran { get; set; } = default!;
}

public class ProcurementCreateDto
{
    public string? JobTypeId { get; set; }

    [Required]
    public int StatusId { get; set; }

    [MaxLength(32)]
    public string? JobType { get; set; }

    [MaxLength(100)]
    public string? ContractType { get; set; }

    [MaxLength(255)]
    public string? JobName { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [MaxLength(16)]
    public string? ProjectRegion { get; set; }

    public decimal? DistanceKm { get; set; }
    public DateTime? PotentialAccrualDate { get; set; }

    [MaxLength(100)]
    public string? SpkNumber { get; set; }

    [MaxLength(100)]
    public string? Wonum { get; set; }

    [MaxLength(100)]
    public string? SpmpNumber { get; set; }

    [MaxLength(100)]
    public string? MemoNumber { get; set; }

    [MaxLength(100)]
    public string? OeNumber { get; set; }

    [MaxLength(255)]
    public string? SelectedVendorName { get; set; }

    [MaxLength(100)]
    public string? VendorSphNumber { get; set; }

    [MaxLength(100)]
    public string? RaNumber { get; set; }

    [MaxLength(64)]
    public string? ProjectCode { get; set; }

    [MaxLength(255)]
    public string? LtcName { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public string? PicOpsUserId { get; set; }
    public string? AnalystHteSignerUserId { get; set; }
    public string? AssistantManagerSignerUserId { get; set; }
    public string? ManagerSignerUserId { get; set; }

    public List<ProcurementDetailDto> Details { get; set; } = [];
    public List<ProcurementOfferDto> Offers { get; set; } = [];
}

public class ProcurementUpdateDto : ProcurementCreateDto
{
    [Required]
    public string ProcurementId { get; set; } = default!;

    public string? ProcNum { get; set; }
}

public class ProcurementResponseDto
{
    public string ProcurementId { get; set; } = default!;
    public string ProcNum { get; set; } = default!;
    public string? Wonum { get; set; }
    public string? JobType { get; set; }
    public string? StatusName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? JobName { get; set; }
}
