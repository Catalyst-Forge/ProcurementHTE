using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class VendorItemOffersDto
        {
            [Required, StringLength(450)]
            public string VendorId { get; set; } = null!;
    
            [MinLength(0)]
            public List<string> Letters { get; set; } = [];
    
            [MinLength(0)]
            public List<string?> LetterDocIds { get; set; } = [];
    
            [MinLength(1)]
            public List<VendorOfferPerItemDto> Items { get; set; } = [];
        }
}

