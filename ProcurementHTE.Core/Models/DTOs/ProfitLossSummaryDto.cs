using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossSummaryDto
        {
            [Required, StringLength(450)]
            public string ProfitLossId { get; set; } = null!;
    
            [Required, StringLength(450)]
            public string ProcurementId { get; set; } = null!;
    
            public decimal TotalOperatorCost { get; set; }
            public decimal TotalRevenue { get; set; }
    
            public decimal? AccrualAmount { get; set; }
            public decimal? RealizationAmount { get; set; }
            public decimal? Distance { get; set; }
    
            [Required, StringLength(450)]
            public string SelectedVendorId { get; set; } = null!;
            public string? SelectedVendorName { get; set; }
    
            [Range(
                typeof(decimal),
                "0",
                "79228162514264337593543950335",
                ErrorMessage = "The field {0} must be a valid non-negative amount."
            )]
            public decimal SelectedFinalOffer { get; set; }
    
            public decimal Profit { get; set; }
            public decimal ProfitPercent { get; set; }
            public DateTime CreatedAt { get; set; }
    
            public List<(
                string ProcOfferId,
                string ItemName,
                int UnitQty,
                decimal BasePrice,
                decimal? TarifAdd,
                decimal? KmPer25,
                decimal? OperatorCost,
                decimal Revenue,
                int? Quantity,
                string? UnitRevenue,
                string? UnitItems
            )> Items { get; set; } = [];
    
            public List<string> SelectedVendorNames { get; set; } = [];
            public List<VendorComparisonDto> VendorComparisons { get; set; } = [];
        }
}

