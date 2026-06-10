using System.Globalization;
using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer : IHtmlTokenReplacer
    {
        private static readonly CultureInfo Id = HtmlTokenFormatter.Id;
        private readonly IProcurementRepository _procRepo;
        private readonly IProfitLossRepository _pnlRepo;
        private readonly IVendorRepository _vendorRepo;
        private readonly UserManager<User> _userManager;
        private readonly IDocumentApprovalRuleRepository _ruleRepo;
        private readonly RoleManager<Role> _roleManager;

        public HtmlTokenReplacer(
            IProcurementRepository procurementRepository,
            IProfitLossRepository pnlRepo,
            IVendorRepository vendorRepo,
            UserManager<User> userManager,
            IDocumentApprovalRuleRepository ruleRepo,
            RoleManager<Role> roleManager
        )
        {
            _procRepo = procurementRepository;
            _pnlRepo = pnlRepo;
            _vendorRepo = vendorRepo;
            _userManager = userManager;
            _ruleRepo = ruleRepo;
            _roleManager = roleManager;
        }
    }
}
