using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossItemInputDto
        {
            [Required, StringLength(450)]
            public string ProcOfferId { get; set; } = null!;
    
            /// <summary>
            /// For PENGANGKUTAN: This is UnitQty (number of trips)
            /// For SEWA_UNIT/MOVING: This is Quantity/Durasi (duration/count)
            /// </summary>
            public int Quantity { get; set; }
    
            /// <summary>
            /// Qty Items from ProcOffer (jumlah unit fisik)
            /// Used by SEWA_UNIT and MOVING to store actual item quantity
            /// </summary>
            public int QtyItems { get; set; }
    
            [Range(
                typeof(decimal),
                "0",
                "79228162514264337593543950335",
                ErrorMessage = "The field {0} must be a valid non-negative number."
            )]
            public decimal TarifAwal { get; set; }
    
            [Range(
                typeof(decimal),
                "0",
                "79228162514264337593543950335",
                ErrorMessage = "The field {0} must be a valid non-negative number."
            )]
            public decimal TarifAdd { get; set; }
    
            [Range(
                typeof(decimal),
                "0",
                "79228162514264337593543950335",
                ErrorMessage = "The field {0} must be a valid non-negative number."
            )]
            public decimal KmPer25 { get; set; }
    
            [Range(
                typeof(decimal),
                "0",
                "79228162514264337593543950335",
                ErrorMessage = "The field {0} must be a valid non-negative number."
            )]
            public decimal OperatorCost { get; set; }
    
            /// <summary>
            /// Unit Revenue - satuan untuk perhitungan revenue (TRIP, HARI, JAM, LSP, KALI)
            /// Will be saved to ProcOffer.UnitRevenue for persistence
            /// </summary>
            public string? UnitRevenue { get; set; }
        }
}

