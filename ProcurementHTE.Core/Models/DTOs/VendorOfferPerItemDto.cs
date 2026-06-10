using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class VendorOfferPerItemDto
    {
        [Required, StringLength(450)]
        public string VendorId { get; set; } = null!;

        public int Round { get; set; }

        [Required, StringLength(450)]
        public string ProcOfferId { get; set; } = null!;

        [MinLength(0)]
        public List<decimal> Prices { get; set; } = [];

        public int Quantity { get; set; }
        public decimal Trip { get; set; }

        /// <summary>
        /// Indicates whether this item is included in the vendor offer.
        /// Items with IsIncluded = false are excluded from the offer.
        /// </summary>
        public bool IsIncluded { get; set; } = true;
    }
}

