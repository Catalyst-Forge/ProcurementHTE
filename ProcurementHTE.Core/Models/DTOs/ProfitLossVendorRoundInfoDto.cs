using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class ProfitLossVendorRoundInfoDto
    {
        public int Round { get; set; }
        public string? LetterNumber { get; set; }
    }
}

