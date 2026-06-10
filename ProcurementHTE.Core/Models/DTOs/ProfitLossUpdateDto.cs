using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossUpdateDto
    {
        [Required, StringLength(450)]
        public string ProfitLossId { get; set; } = null!;

        [Required, StringLength(450)]
        public string ProcurementId { get; set; } = null!;

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount."
        )]
        public decimal? AccrualAmount { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "79228162514264337593543950335",
            ErrorMessage = "The field {0} must be a valid non-negative amount."
        )]
        public decimal? RealizationAmount { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? Distance { get; set; }

        public DateTime? TglMulaiSewa { get; set; }

        public DateTime? TglMulaiMoving { get; set; }

        [MinLength(1)]
        public List<ProfitLossItemInputDto> Items { get; set; } = [];

        public List<string> SelectedVendorIds { get; set; } = [];

        public List<VendorItemOffersDto> Vendors { get; set; } = [];

        public byte[]? RowVersion { get; set; }
    }
}

