using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossNegotiationTablesDto
        {
            public string ProfitLossId { get; set; } = null!;
            public string ProcurementId { get; set; } = null!;
            public List<ProfitLossVendorNegotiationTableDto> Vendors { get; set; } = [];
        }
}

