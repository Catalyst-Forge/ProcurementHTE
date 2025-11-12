using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs {
    public class WorkOrderDetailDto {
        [MaxLength(255)]
        public string? ItemName { get; set; }
        public int? Quantity { get; set; }
        public string? Unit { get; set; }
    }

    public class WorkOrderOfferDto {
        [Required]
        public string ItemPenawaran { get; set; } = default!;
    }

    public class WorkOrderCreateDto {
        [Required]
        public string WoTypeId { get; set; } = default!;

        [Required]
        public int StatusId { get; set; }

        public ProcurementType ProcurementType { get; set; }

        public string? WoNumLetter { get; set; }
        public DateTime? DateLetter { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? WorkOrderLetter { get; set; }
        public string? WBS { get; set; }
        public string? GlAccount { get; set; }
        public DateTime? DateRequired { get; set; }
        public string? Description { get; set; }
        public string? Note { get; set; }
        public string? Requester { get; set; }
        public string? Approved { get; set; }
        public string? XS1 { get; set; }
        public string? XS2 { get; set; }
        public string? XS3 { get; set; }
        public string? XS4 { get; set; }

        public List<WorkOrderDetailDto> Details { get; set; } = [];
        public List<WorkOrderOfferDto> Offers { get; set; } = [];
    }

    public class WorkOrderUpdateDto {
        [Required]
        public string WorkOrderId { get; set; } = default!;
        public string? WoTypeId { get; set; }
        public int? StatusId { get; set; }
        public ProcurementType? ProcurementType { get; set; }

        public string? WoNumLetter { get; set; }
        public DateTime? DateLetter { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? WorkOrderLetter { get; set; }
        public string? WBS { get; set; }
        public string? GlAccount { get; set; }
        public DateTime? DateRequired { get; set; }
        public string? Description { get; set; }
        public string? Note { get; set; }
        public string? Requester { get; set; }
        public string? Approved { get; set; }
        public string? XS1 { get; set; }
        public string? XS2 { get; set; }
        public string? XS3 { get; set; }
        public string? XS4 { get; set; }

        public List<WorkOrderDetailDto> Details { get; set; } = [];
        public List<WorkOrderOfferDto> Offers { get; set; } = [];
    }

    public class WorkOrderResponseDto {
        public string WorkOrderId { get; set; } = default!;
        public string? WoNum { get; set; }
        public string? WoTypeName { get; set; }
        public string? StatusName { get; set; }
        public ProcurementType ProcurementType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DateLetter { get; set; }
    }
}
