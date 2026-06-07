using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Utils;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProcurementRepository
    {
        private IQueryable<Procurement> BuildBaseQuery()
        {
            return _context
                .Procurements.Include(procurement => procurement.ProcOffers)
                .Include(procurement => procurement.Status)
                .Include(procurement => procurement.JobType)
                .Include(procurement => procurement.User)
                .Include(procurement => procurement.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .AsSplitQuery()
                .AsNoTracking();
        }

        private static IQueryable<Procurement> ApplySearchFilter(
            IQueryable<Procurement> query,
            string searchTerm,
            ISet<string> fields
        )
        {
            var byProcNum = fields.Contains("ProcNum", StringComparer.OrdinalIgnoreCase);
            var byWonum = fields.Contains("Wonum", StringComparer.OrdinalIgnoreCase);
            var byJobName = fields.Contains("JobName", StringComparer.OrdinalIgnoreCase);
            var byProjectCode = fields.Contains("ProjectCode", StringComparer.OrdinalIgnoreCase);
            var byStatus = fields.Contains("Status", StringComparer.OrdinalIgnoreCase);
            var like = $"%{searchTerm}%";

            return query.Where(procurement =>
                (
                    byProcNum
                    && procurement.ProcNum != null
                    && EF.Functions.Like(procurement.ProcNum, like)
                )
                || (
                    byWonum
                    && procurement.Wonum != null
                    && EF.Functions.Like(procurement.Wonum, like)
                )
                || (
                    byJobName
                    && procurement.JobName != null
                    && EF.Functions.Like(procurement.JobName, like)
                )
                || (
                    byProjectCode
                    && procurement.ProjectCode != null
                    && EF.Functions.Like(procurement.ProjectCode, like)
                )
                || (
                    byStatus
                    && procurement.Status != null
                    && EF.Functions.Like(procurement.Status.StatusName, like)
                )
            );
        }

        private async Task<string> GenerateNextProcNumAsync()
        {
            var lastProcNum = await GetLastProcNumAsync(PROC_PREFIX);
            return SequenceNumberGenerator.NumId(PROC_PREFIX, lastProcNum);
        }

        private async Task<string?> GetLastProcNumAsync(string prefix)
        {
            return await _context
                .Procurements.Where(procurement => procurement.ProcNum!.StartsWith(prefix))
                .OrderByDescending(procurement => procurement.ProcNum)
                .Select(procurement => procurement.ProcNum)
                .FirstOrDefaultAsync();
        }

        private static List<ProcDetail> FilterValidDetails(List<ProcDetail>? details) =>
            (details ?? [])
                .Where(detail =>
                    !string.IsNullOrWhiteSpace(detail.ItemName)
                    && detail.Quantity.HasValue
                    && detail.Quantity.Value > 0
                )
                .ToList();

        private static List<ProcOffer> FilterValidOffers(List<ProcOffer>? offers) =>
            (offers ?? []).Where(offer => !string.IsNullOrWhiteSpace(offer.ItemPenawaran)).ToList();

        private static void AssignProcurementIdToDetails(
            string procurementId,
            List<ProcDetail> details
        )
        {
            foreach (var detail in details)
                detail.ProcurementId = procurementId;
        }

        private static void AssignProcurementIdToOffers(
            string procurementId,
            List<ProcOffer> offers
        )
        {
            foreach (var offer in offers)
                offer.ProcurementId = procurementId;
        }

        private static bool IsUniqueProcNumViolation(DbUpdateException ex) =>
            ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
            && sqlEx.Number == 2627
            && (sqlEx.Message?.Contains("AK_Procurements_ProcNum") ?? false);
    }
}
