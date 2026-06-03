namespace ProcurementHTE.Core.Constants
{
    /// <summary>
    /// Constants for approval workflow based on CT (Contract Total / Final Offer PNL)
    /// </summary>
    public static class ApprovalConstants
    {
        // CT Thresholds (in Rupiah)
        // Approval levels based on CT:
        // - CT ≤ 500M: Analyst → Asst Manager → Manager (Level 4)
        // - CT ≤ 5B: + VP (Level 5)
        // - CT ≤ 10B: + VP → OpDir (Level 6)
        // - CT > 10B: + VP → OpDir → PresDir (Level 7)

        /// <summary>
        /// 500 Juta - Threshold for VP approval
        /// CT > this value requires VP approval
        /// </summary>
        public const decimal CT_THRESHOLD_VP = 500_000_000m;

        /// <summary>
        /// 5 Miliar - Threshold for Operation Director approval
        /// CT > this value requires Operation Director approval
        /// </summary>
        public const decimal CT_THRESHOLD_OP_DIR = 5_000_000_000m;

        /// <summary>
        /// 10 Miliar - Threshold for President Director approval
        /// CT > this value requires President Director approval
        /// </summary>
        public const decimal CT_THRESHOLD_PRES_DIR = 10_000_000_000m;

        // Approval Levels (corresponds to final status before ISPA)
        // Level 4 = Manager (default, CT ≤ 500M)
        // Level 5 = VP (CT > 500M)
        // Level 6 = OpDir (CT > 5B)
        // Level 7 = PresDir (CT > 10B)

        /// <summary>
        /// Base approval level (Analyst → Asst Manager → Manager)
        /// </summary>
        public const int APPROVAL_LEVEL_MANAGER = 4;

        /// <summary>
        /// Approval level including VP
        /// </summary>
        public const int APPROVAL_LEVEL_VP = 5;

        /// <summary>
        /// Approval level including Operation Director
        /// </summary>
        public const int APPROVAL_LEVEL_OP_DIR = 6;

        /// <summary>
        /// Approval level including President Director
        /// </summary>
        public const int APPROVAL_LEVEL_PRES_DIR = 7;

        /// <summary>
        /// Calculate required approval level based on CT value
        /// </summary>
        /// <param name="ct">Contract Total (Final Offer PNL) in Rupiah</param>
        /// <returns>Approval level (4-7)</returns>
        public static int GetRequiredApprovalLevel(decimal ct)
        {
            if (ct > CT_THRESHOLD_PRES_DIR)
                return APPROVAL_LEVEL_PRES_DIR;
            if (ct > CT_THRESHOLD_OP_DIR)
                return APPROVAL_LEVEL_OP_DIR;
            if (ct > CT_THRESHOLD_VP)
                return APPROVAL_LEVEL_VP;
            return APPROVAL_LEVEL_MANAGER;
        }

        /// <summary>
        /// Check if VP approval is required based on CT
        /// </summary>
        public static bool RequiresVpApproval(decimal ct) => ct > CT_THRESHOLD_VP;

        /// <summary>
        /// Check if Operation Director approval is required based on CT
        /// </summary>
        public static bool RequiresOpDirApproval(decimal ct) => ct > CT_THRESHOLD_OP_DIR;

        /// <summary>
        /// Check if President Director approval is required based on CT
        /// </summary>
        public static bool RequiresPresDirApproval(decimal ct) => ct > CT_THRESHOLD_PRES_DIR;

        /// <summary>
        /// Get total steps for progress bar based on approval level
        /// Base steps: OnCreateDP3 + 3 approvals + ISPA + Hardcopy + PO + DonePO = 8
        /// With VP: 9, With OpDir: 10, With PresDir: 11
        /// </summary>
        public static int GetTotalSteps(int approvalLevel)
        {
            return approvalLevel switch
            {
                APPROVAL_LEVEL_VP => 9,
                APPROVAL_LEVEL_OP_DIR => 10,
                APPROVAL_LEVEL_PRES_DIR => 11,
                _ => 8 // Default: Manager level
            };
        }
    }
}
