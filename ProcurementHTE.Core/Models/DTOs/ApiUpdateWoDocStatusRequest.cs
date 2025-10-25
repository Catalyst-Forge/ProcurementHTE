using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcurementHTE.Core.Models.DTOs
{
    public record ApiUpdateWoDocStatusRequest(
        string Status, // target status
        string? Reason = null, // optional note
        string? ApprovedByUserId = null // optional, isi user id yang approve/reject
    );
}
