using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Services;

namespace ProcurementHTE.Web.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ProcurementService>();
        services.AddScoped<IProcurementQueryService>(sp => sp.GetRequiredService<ProcurementService>());
        services.AddScoped<IProcurementCommandService>(sp => sp.GetRequiredService<ProcurementService>());
        services.AddScoped<IProcurementWorkflowService>(sp => sp.GetRequiredService<ProcurementService>());
        services.AddScoped<IPurchaseRequisitionService, PurchaseRequisitionService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IJobTypeService, JobTypesService>();
        services.AddScoped<IDocumentTypeService, DocumentTypeService>();
        services.AddScoped<IProfitLossService, ProfitLossService>();
        services.AddScoped<IVendorOfferService, VendorOfferService>();
        services.AddScoped<IProcDocumentService, ProcDocumentService>();
        services.AddScoped<IJobTypeDocumentService, JobTypeDocumentService>();
        services.AddScoped<IJobTypeDocumentAdminService, JobTypeDocumentAdminService>();
        services.AddScoped<IDocumentApprovalRuleService, DocumentApprovalRuleService>();
        services.AddScoped<IDocumentApprovalsService, DocumentApprovalsService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITemplateProvider, FileSystemTemplateProvider>();
        services.AddScoped<IHtmlTokenReplacer, HtmlTokenReplacer>();
        services.AddScoped<IJobTypeCalculationService, JobTypeCalculationService>();
        services.AddScoped<ILdpService, LdpService>();
        services.AddScoped<IProcurementTrackingService, ProcurementTrackingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IProcurementDocumentGenerator, ProcurementDocumentGenerator>();

        return services;
    }
}
