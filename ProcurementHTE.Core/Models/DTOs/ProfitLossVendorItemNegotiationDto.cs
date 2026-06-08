using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossVendorItemNegotiationDto
        {
            public string ProcOfferId { get; set; } = null!;
            public string ItemName { get; set; } = null!;
            public int Quantity { get; set; }
            public int Trip { get; set; }
            public List<decimal?> PricesPerRound { get; set; } = [];
            public decimal? FinalPrice { get; set; }
            public decimal? Total { get; set; }
        }
}

