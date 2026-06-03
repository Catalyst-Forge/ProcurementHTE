using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels;

public class PurchaseRequisitionCreateViewModel
{
    [Required(ErrorMessage = "PR Number is required")]
    [MaxLength(100)]
    [Display(Name = "PR Number")]
    public string PRNumber { get; set; } = null!;

    [Required(ErrorMessage = "Request Date is required")]
    [Display(Name = "Request Date")]
    [DataType(DataType.Date)]
    public DateTime RequestDate { get; set; } = DateTime.Now;

    [MaxLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Document file is required")]
    [Display(Name = "PR Document")]
    public IFormFile DocumentFile { get; set; } = null!;

    [Display(Name = "Selected Procurements")]
    public List<string> ProcurementIds { get; set; } = [];
}

public class PurchaseRequisitionEditViewModel
{
    public string PrId { get; set; } = null!;

    [Required(ErrorMessage = "PR Number is required")]
    [MaxLength(100)]
    [Display(Name = "PR Number")]
    public string PRNumber { get; set; } = null!;

    [Required(ErrorMessage = "Request Date is required")]
    [Display(Name = "Request Date")]
    [DataType(DataType.Date)]
    public DateTime RequestDate { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "PR Document")]
    public IFormFile? DocumentFile { get; set; }

    public string? ExistingDocumentFileName { get; set; }

    [Display(Name = "Selected Procurements")]
    public List<string> ProcurementIds { get; set; } = [];
}

public class PurchaseRequisitionListViewModel
{
    public string PrId { get; set; } = null!;
    public string PrNumber { get; set; } = null!;
    public DateTime RequestDate { get; set; }
    public string? Description { get; set; }
    public string? DocumentFileName { get; set; }
    public int ProcurementCount { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PurchaseRequisitionDetailsViewModel
{
    public string PrId { get; set; } = null!;
    public string PrNumber { get; set; } = null!;
    public DateTime RequestDate { get; set; }
    public string? Description { get; set; }
    public string? DocumentFileName { get; set; }
    public string? DocumentFilePath { get; set; }
    public long? DocumentFileSize { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<ProcurementWithDocsViewModel> Procurements { get; set; } = [];
}

public class ProcurementWithDocsViewModel
{
    public string ProcurementId { get; set; } = null!;
    public string? ProcNum { get; set; }
    public string? Wonum { get; set; }
    public string? JobName { get; set; }
    public string? JobTypeName { get; set; }
    public string? StatusName { get; set; }
    public string? Category { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? VendorName { get; set; }

    public List<RequiredDocItemDto> RequiredDocuments { get; set; } = [];
    public List<VendorRoundLetter> RoundLetters { get; set; } = [];

    public int CompletedDocs { get; set; }
    public int TotalDocs { get; set; }
    public int ProgressPercent => TotalDocs > 0 ? (CompletedDocs * 100 / TotalDocs) : 0;
}
