using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService : IProfitLossService
    {
        private readonly IProfitLossRepository _pnlRepository;
        private readonly IVendorOfferRepository _voRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly IVendorRoundLetterRepository _roundLetterRepository;
        private readonly IJobTypeCalculationService _jobTypeCalc;

        public ProfitLossService(
            IProfitLossRepository pnlRepository,
            IVendorOfferRepository voRepository,
            IVendorRepository vendorRepository,
            IVendorRoundLetterRepository roundLetterRepository,
            IJobTypeCalculationService jobTypeCalculationService
        )
        {
            _pnlRepository = pnlRepository;
            _voRepository = voRepository;
            _vendorRepository = vendorRepository;
            _roundLetterRepository = roundLetterRepository;
            _jobTypeCalc = jobTypeCalculationService;
        }
    }
}
