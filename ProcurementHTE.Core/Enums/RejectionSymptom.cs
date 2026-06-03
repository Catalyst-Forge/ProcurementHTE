using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Enums;

/// <summary>
/// Flags enum representing reasons for procurement rejection.
/// Multiple symptoms can be selected during rejection.
/// </summary>
[Flags]
public enum RejectionSymptom
{
    None = 0,

    // ============================================
    // DATA ISSUES - Target: PIC Ops / Creator
    // Requires revision of procurement data
    // ============================================

    /// <summary>
    /// Nilai PnL tidak sesuai atau anomali
    /// </summary>
    [Display(Name = "Nilai PnL Anomali", GroupName = "Data Issue")]
    PnLValueAnomaly = 1,

    /// <summary>
    /// Data tanggal (mulai, selesai, dokumen) tidak sesuai
    /// </summary>
    [Display(Name = "Data Tanggal Tidak Sesuai", GroupName = "Data Issue")]
    DateDataIncorrect = 2,

    /// <summary>
    /// Data vendor atau SPK tidak valid
    /// </summary>
    [Display(Name = "Vendor/SPK Tidak Valid", GroupName = "Data Issue")]
    VendorOrSpkInvalid = 4,

    /// <summary>
    /// Data pekerjaan (job name, job type, dll) salah
    /// </summary>
    [Display(Name = "Data Pekerjaan Salah", GroupName = "Data Issue")]
    JobDataIncorrect = 8,

    /// <summary>
    /// Data kontrak atau nilai kontrak salah
    /// </summary>
    [Display(Name = "Data Kontrak Salah", GroupName = "Data Issue")]
    ContractDataIncorrect = 16,

    // ============================================
    // PR/DOCUMENT ISSUES - Target: APPO
    // Requires revision of PR linking or documents
    // ============================================

    /// <summary>
    /// ⚠️ SPECIAL CASE: Procurement tidak bisa digabung ke 1 PR.
    /// Will trigger UNLINK from PR and require fresh pickup.
    /// </summary>
    [Display(Name = "Tidak Bisa Digabung ke 1 PR", GroupName = "PR Issue")]
    PRCannotBeCombined = 32,

    /// <summary>
    /// Linking procurement ke PR salah
    /// </summary>
    [Display(Name = "Linking PR Salah", GroupName = "PR Issue")]
    PRLinkingIncorrect = 64,

    /// <summary>
    /// Dokumen tidak lengkap
    /// </summary>
    [Display(Name = "Dokumen Tidak Lengkap", GroupName = "PR Issue")]
    DocumentIncomplete = 128,

    /// <summary>
    /// Format dokumen salah
    /// </summary>
    [Display(Name = "Format Dokumen Salah", GroupName = "PR Issue")]
    DocumentFormatInvalid = 256,

    /// <summary>
    /// Dokumen expired atau tidak berlaku
    /// </summary>
    [Display(Name = "Dokumen Expired", GroupName = "PR Issue")]
    DocumentExpired = 512,

    // ============================================
    // OTHER
    // ============================================

    /// <summary>
    /// Alasan lain (wajib isi catatan)
    /// </summary>
    [Display(Name = "Lainnya (jelaskan di catatan)", GroupName = "Lainnya")]
    Other = 1024
}

/// <summary>
/// Extension methods for RejectionSymptom enum
/// </summary>
public static class RejectionSymptomExtensions
{
    /// <summary>
    /// All symptoms that require data revision by PIC Ops/Creator
    /// </summary>
    public static RejectionSymptom DataIssues =>
        RejectionSymptom.PnLValueAnomaly |
        RejectionSymptom.DateDataIncorrect |
        RejectionSymptom.VendorOrSpkInvalid |
        RejectionSymptom.JobDataIncorrect |
        RejectionSymptom.ContractDataIncorrect;

    /// <summary>
    /// All symptoms that require PR/document revision by APPO
    /// </summary>
    public static RejectionSymptom PRIssues =>
        RejectionSymptom.PRCannotBeCombined |
        RejectionSymptom.PRLinkingIncorrect |
        RejectionSymptom.DocumentIncomplete |
        RejectionSymptom.DocumentFormatInvalid |
        RejectionSymptom.DocumentExpired;

    /// <summary>
    /// Check if symptoms contain any data issues
    /// </summary>
    public static bool HasDataIssues(this RejectionSymptom symptoms)
        => (symptoms & DataIssues) != 0;

    /// <summary>
    /// Check if symptoms contain any PR issues
    /// </summary>
    public static bool HasPRIssues(this RejectionSymptom symptoms)
        => (symptoms & PRIssues) != 0;

    /// <summary>
    /// Check if symptoms contain the special "cannot be combined" flag
    /// </summary>
    public static bool HasPRCannotBeCombined(this RejectionSymptom symptoms)
        => (symptoms & RejectionSymptom.PRCannotBeCombined) != 0;

    /// <summary>
    /// Get data issue symptoms only
    /// </summary>
    public static RejectionSymptom GetDataIssues(this RejectionSymptom symptoms)
        => symptoms & DataIssues;

    /// <summary>
    /// Get PR issue symptoms only
    /// </summary>
    public static RejectionSymptom GetPRIssues(this RejectionSymptom symptoms)
        => symptoms & PRIssues;

    /// <summary>
    /// Get display names of all selected symptoms
    /// </summary>
    public static IEnumerable<string> GetSelectedDisplayNames(this RejectionSymptom symptoms)
    {
        foreach (RejectionSymptom value in Enum.GetValues<RejectionSymptom>())
        {
            if (value == RejectionSymptom.None) continue;
            if ((symptoms & value) == value)
            {
                var field = typeof(RejectionSymptom).GetField(value.ToString());
                var attr = field?.GetCustomAttributes(typeof(DisplayAttribute), false)
                    .FirstOrDefault() as DisplayAttribute;
                yield return attr?.Name ?? value.ToString();
            }
        }
    }
}
