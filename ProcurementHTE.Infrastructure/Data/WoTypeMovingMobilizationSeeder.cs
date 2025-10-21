using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using System.Reflection.Metadata;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class WoTypeMovingMobilizationSeeder
    {
        public static async Task SeedAsync(AppDbContext context, RoleManager<Role> roleManager)
        {
            if (await context.DocumentTypes.AnyAsync())
                return;

            var woType = await context.WoTypes.FirstOrDefaultAsync(t => t.TypeName == "Moving & Mobilization");
            // Cek apakah tipe sudah ada
            if (woType == null) {
                // ---- 1️⃣ WoType
                woType = new WoTypes
                {
                    TypeName = "Moving & Mobilization",
                    Description = "Konfigurasi dokumen untuk proses Moving & Mobilization"
                };
                await context.WoTypes.AddAsync(woType);
                await context.SaveChangesAsync();
            }

            // ---- 2️⃣ DocumentType
            var documentTypes = new List<DocumentType>
            {
                new() { Name = "Memorandum", Description = "Memorandum internal; approval Manager Transport & Logistic" },
                new() { Name = "Permintaan Pekerjaan", Description = "Di luar role aplikasi; tidak butuh approver/generate" },
                new() { Name = "Service Order", Description = "Approval Manager Transport & Logistic & digenerate oleh system" },
                new() { Name = "Market Survey", Description = "Upload dari HTE; mempengaruhi progress WO" },
                new() { Name = "Profit & Loss", Description = "Di-generate; approval Analyst HTE & LTS, Assistant Manager HTE, Manager Transport & Logistic" },
                new() { Name = "Surat Perintah Mulai Pekerjaan (SPMP)", Description = "Approval Manager Transport & Logistic" },
                new() { Name = "Surat Penawaran Harga", Description = "Upload dari HTE; mempengaruhi progress WO" },
                new() { Name = "Surat Negosiasi Harga", Description = "Upload dari HTE; mempengaruhi progress WO" },
                new() { Name = "Rencana Kerja dan Syarat-Syarat (RKS)", Description = "Generate; approval Assistant Manager HTE dan Manager Transport & Logistic" },
                new() { Name = "Risk Assessment (RA)", Description = "Jika pengadaan jasa; approval HSE, Assistant Manager HTE, Manager Transport & Logistic" },
                new() { Name = "Owner Estimate (OE)", Description = "Generate; approval Assistant Manager HTE dan Manager Transport & Logistic" },
                new() { Name = "Bill of Quantity (BOQ)", Description = "Di-generate oleh sistem" },
            };
            await context.DocumentTypes.AddRangeAsync(documentTypes);
            await context.SaveChangesAsync();

            // ---- 3️⃣ WoTypeDocuments
            var wtd = new List<WoTypeDocuments>
            {
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[0].DocumentTypeId, Sequence = 1,  IsMandatory = true,  IsGenerated = false, IsUploadRequired = false, RequiresApproval = true,  Note = "Memorandum approval Manager Transport & Logistic" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[1].DocumentTypeId, Sequence = 2,  IsMandatory = false, IsGenerated = false, IsUploadRequired = false, RequiresApproval = false, Note = "Dokumen eksternal; tidak di-generate; tidak perlu approval" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[2].DocumentTypeId, Sequence = 3,  IsMandatory = true,  IsGenerated = false, IsUploadRequired = false, RequiresApproval = true,  Note = "Service Order approval Manager Transport & Logistic" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[3].DocumentTypeId, Sequence = 4,  IsMandatory = false, IsGenerated = false, IsUploadRequired = true,  RequiresApproval = false, Note = "Market Survey upload dari HTE" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[4].DocumentTypeId, Sequence = 5,  IsMandatory = true,  IsGenerated = true,  IsUploadRequired = false, RequiresApproval = true,  Note = "Profit & Loss generate; approval Analyst HTE & LTS -> Assistant Manager HTE -> Manager Transport & Logistic" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[5].DocumentTypeId, Sequence = 6,  IsMandatory = true,  IsGenerated = false, IsUploadRequired = false, RequiresApproval = true,  Note = "SPMP approval Manager Transport & Logistic" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[6].DocumentTypeId, Sequence = 7,  IsMandatory = false, IsGenerated = false, IsUploadRequired = true,  RequiresApproval = false, Note = "SPH upload dari HTE" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[7].DocumentTypeId, Sequence = 8,  IsMandatory = false, IsGenerated = false, IsUploadRequired = true,  RequiresApproval = false, Note = "SNH upload dari HTE" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[8].DocumentTypeId, Sequence = 9,  IsMandatory = true,  IsGenerated = true,  IsUploadRequired = false, RequiresApproval = true,  Note = "RKS approval Assistant Manager HTE -> Manager Transport & Logistic" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[9].DocumentTypeId, Sequence = 10, IsMandatory = true,  IsGenerated = false, IsUploadRequired = true,  RequiresApproval = true,  Note = "Risk Assessment approval HSE -> Assistant Manager HTE -> Manager Transport & Logistic" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[10].DocumentTypeId, Sequence = 11, IsMandatory = true,  IsGenerated = true,  IsUploadRequired = false, RequiresApproval = true,  Note = "Owner Estimate approval Assistant Manager HTE -> Manager Transport & Logistic" },
                new() { WoTypeId = woType.WoTypeId, DocumentTypeId = documentTypes[11].DocumentTypeId, Sequence = 12, IsMandatory = true,  IsGenerated = true,  IsUploadRequired = false, RequiresApproval = false, Note = "BOQ generate otomatis oleh sistem" },
            };
            await context.WoTypesDocuments.AddRangeAsync(wtd);
            await context.SaveChangesAsync();

            // ---- 4️⃣ Role lookup helper
            async Task<string> GetRoleIdAsync(string roleName)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null)
                    throw new Exception($"Role '{roleName}' belum ada di database.");
                return role.Id;
            }

            // ---- 5️⃣ DocumentApprovals
            var approvals = new List<DocumentApprovals>
            {
                // Memorandum
                new() { WoTypeDocumentId = wtd[0].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Manager Transport & Logistic"), Level = 1, SequenceOrder = 1 },

                // Service Order
                new() { WoTypeDocumentId = wtd[2].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Manager Transport & Logistic"), Level = 1, SequenceOrder = 1 },

                // SPMP
                new() { WoTypeDocumentId = wtd[5].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Manager Transport & Logistic"), Level = 1, SequenceOrder = 1 },

                // Profit & Loss
                new() { WoTypeDocumentId = wtd[4].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Analyst HTE & LTS"), Level = 1, SequenceOrder = 1 },
                new() { WoTypeDocumentId = wtd[4].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Assistant Manager HTE"), Level = 2, SequenceOrder = 2 },
                new() { WoTypeDocumentId = wtd[4].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Manager Transport & Logistic"), Level = 3, SequenceOrder = 3 },

                // RKS
                new() { WoTypeDocumentId = wtd[8].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Assistant Manager HTE"), Level = 1, SequenceOrder = 1 },
                new() { WoTypeDocumentId = wtd[8].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Manager Transport & Logistic"), Level = 2, SequenceOrder = 2 },

                // Risk Assessment
                new() { WoTypeDocumentId = wtd[9].WoTypeDocumentId, RoleId = await GetRoleIdAsync("HSE"), Level = 1, SequenceOrder = 1 },
                new() { WoTypeDocumentId = wtd[9].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Assistant Manager HTE"), Level = 2, SequenceOrder = 2 },
                new() { WoTypeDocumentId = wtd[9].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Manager Transport & Logistic"), Level = 3, SequenceOrder = 3 },

                // Owner Estimate
                new() { WoTypeDocumentId = wtd[10].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Assistant Manager HTE"), Level = 1, SequenceOrder = 1 },
                new() { WoTypeDocumentId = wtd[10].WoTypeDocumentId, RoleId = await GetRoleIdAsync("Manager Transport & Logistic"), Level = 2, SequenceOrder = 2 },
            };

            await context.DocumentApprovals.AddRangeAsync(approvals);
            await context.SaveChangesAsync();
        }
    }
}
