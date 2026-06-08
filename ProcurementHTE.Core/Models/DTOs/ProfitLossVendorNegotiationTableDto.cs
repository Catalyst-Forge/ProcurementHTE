using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossVendorNegotiationTableDto
        {
            public string VendorId { get; set; } = null!;
            public string VendorName { get; set; } = null!;
            public int MaxRound { get; set; }
            public List<ProfitLossVendorRoundInfoDto> Rounds { get; set; } = [];
            public List<ProfitLossVendorItemNegotiationDto> Items { get; set; } = [];
            public decimal GrandTotal { get; set; }
            public bool IsSelectedVendor { get; set; }
        }
}

