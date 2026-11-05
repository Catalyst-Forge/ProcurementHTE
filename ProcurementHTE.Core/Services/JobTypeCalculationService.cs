using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class JobTypeCalculationService : IJobTypeCalculationService
    {
        // UnitType GUIDs dari seed data
        private const string UNIT_TYPE_HARI = "11111111-1111-1111-1111-111111111111";
        private const string UNIT_TYPE_JAM = "22222222-2222-2222-2222-222222222222";
        private const string UNIT_TYPE_LSP = "33333333-3333-3333-3333-333333333333";
        private const string UNIT_TYPE_TRIP = "44444444-4444-4444-4444-444444444444";
        private const string UNIT_TYPE_KALI = "55555555-5555-5555-5555-555555555555";

        // JobType TypeNames (from database)
        private const string JOBTYPE_PENGANGKUTAN = "Angkutan";
        private const string JOBTYPE_SEWA_UNIT = "StandBy";
        private const string JOBTYPE_MOVING = "Moving";

        public List<string> GetAvailableUnitTypeIds(string jobTypeName)
        {
            return jobTypeName switch
            {
                JOBTYPE_PENGANGKUTAN => new List<string> { UNIT_TYPE_TRIP }, // Fixed: TRIP only
                JOBTYPE_SEWA_UNIT => new List<string>
                {
                    UNIT_TYPE_HARI,
                    UNIT_TYPE_JAM,
                    UNIT_TYPE_LSP,
                },
                JOBTYPE_MOVING => new List<string> { UNIT_TYPE_TRIP, UNIT_TYPE_KALI },
                _ => new List<string> { UNIT_TYPE_TRIP }, // Default fallback
            };
        }

        public string GetDefaultUnitTypeId(string jobTypeName)
        {
            return jobTypeName switch
            {
                JOBTYPE_PENGANGKUTAN => UNIT_TYPE_TRIP,
                JOBTYPE_SEWA_UNIT => UNIT_TYPE_HARI,
                JOBTYPE_MOVING => UNIT_TYPE_TRIP,
                _ => UNIT_TYPE_TRIP,
            };
        }

        public decimal CalculateVendorTotal(List<VendorOffer> offers)
        {
            return offers
                .GroupBy(o => o.ProcOfferId)
                .Sum(g =>
                {
                    // Ambil harga terendah dari semua round
                    var bestPrice = g.Min(o => o.Price);
                    var representative = g.First();

                    return bestPrice * representative.QuantityItem * representative.QuantityOfUnit;
                });
        }

        public void ValidateRequiredFields(ProfitLoss profitLoss, string jobTypeName)
        {
            switch (jobTypeName)
            {
                case JOBTYPE_SEWA_UNIT:
                    if (!profitLoss.TglMulaiSewa.HasValue)
                    {
                        throw new InvalidOperationException(
                            "Tanggal Mulai Sewa wajib diisi untuk Sewa Unit"
                        );
                    }
                    break;

                case JOBTYPE_MOVING:
                    if (!profitLoss.TglMulaiMoving.HasValue)
                    {
                        throw new InvalidOperationException(
                            "Tanggal Mulai Moving wajib diisi untuk Moving"
                        );
                    }
                    break;
            }
        }

        public string GetVendorOfferUnitTypeId(string jobTypeName, string? billingUnitTypeId)
        {
            if (jobTypeName == JOBTYPE_PENGANGKUTAN)
            {
                // PENGANGKUTAN always uses TRIP
                return UNIT_TYPE_TRIP;
            }
            else
            {
                // SEWA_UNIT & MOVING inherit from billing
                return billingUnitTypeId ?? GetDefaultUnitTypeId(jobTypeName);
            }
        }

        public bool IsDistanceUsed(string jobTypeName)
        {
            return jobTypeName switch
            {
                JOBTYPE_PENGANGKUTAN => true, // Required for calculation
                JOBTYPE_MOVING => true, // Info only (not for calculation)
                _ => false,
            };
        }

        public bool IsDistanceRequiredForCalculation(string jobTypeName)
        {
            return jobTypeName == JOBTYPE_PENGANGKUTAN;
        }
    }
}
