using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProcurementHTE.Core.Models
{
    public class DocumentApprovals
    {
        [Key]
        public string DocumentApprovalId { get; set; } = Guid.NewGuid().ToString();

        [Range(1, int.MaxValue, ErrorMessage = "Level must be at least 1.")]
        public int Level { get; set; }

        // Foreign Keys
        public string RoleId { get; set; } = null!;
        public string JobTypeDocumentId { get; set; } = null!;

        [ForeignKey("RoleId")]
        [ValidateNever]
        public Role Role { get; set; } = default!;

        [ForeignKey("JobTypeDocumentId")]
        [ValidateNever]
        public JobTypeDocuments JobTypeDocument { get; set; } = default!;
    }
}
