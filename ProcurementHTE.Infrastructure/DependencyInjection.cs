using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Options;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Repositories;
using ProcurementHTE.Infrastructure.Services;
using ProcurementHTE.Infrastructure.Storage;

namespace ProcurementHTE.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // ---------------- DB ----------------
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
        );

        // ------------- Options Binding -------------
        services.Configure<ObjectStorageOptions>(configuration.GetSection("ObjectStorage"));
        services.Configure<EmailSenderOptions>(configuration.GetSection("EmailSender"));
        services.Configure<SmsSenderOptions>(configuration.GetSection("SmsSender"));

        // ------------- Storage & Utilities -------------
        services.AddSingleton<IObjectStorage, MinioStorage>();
        services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
        services.AddHttpClient("SmsProvider");

        // ------------- Repositories -------------
        services.AddScoped<IProcurementRepository, ProcurementRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<ITenderRepository, TenderRepository>();
        services.AddScoped<IJobTypeRepository, JobTypesRepository>();
        services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
        services.AddScoped<IProfitLossRepository, ProfitLossRepository>();
        services.AddScoped<IVendorOfferRepository, VendorOfferRepository>();
        services.AddScoped<IVendorRoundLetterRepository, VendorRoundLetterRepository>();
        services.AddScoped<IProcDocumentRepository, ProcDocumentRepository>();
        services.AddScoped<IJobTypeDocumentRepository, JobTypeDocumentRepository>();
        services.AddScoped<IProcDocumentApprovalRepository, ProcDocumentApprovalRepository>();
        services.AddScoped<IProcDocApprovalFlowRepository, ProcDocApprovalFlowRepository>();
        services.AddScoped<IApprovalRepository, ApprovalRepository>();
        services.AddScoped<IDocumentApprovalRuleRepository, DocumentApprovalRuleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IUserSecurityLogRepository, UserSecurityLogRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IDocumentApprovalsRepository, DocumentApprovalsRepository>();
        services.AddScoped<IJobTypeDocumentAdminRepository, JobTypeDocumentAdminRepository>();

        // ------------- Query Services -------------
        services.AddScoped<IProcurementDocumentQuery, ProcurementDocumentQuery>();

        // ------------- Cross-cutting Infrastructure -------------
        services.AddSingleton<IEmailSender>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EmailSenderOptions>>().Value;
            if (opts.UseDevelopmentMode)
                return ActivatorUtilities.CreateInstance<ConsoleEmailSender>(sp);
            return ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp);
        });

        services.AddSingleton<ISmsSender>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<SmsSenderOptions>>().Value;
            if (opts.UseDevelopmentMode)
                return ActivatorUtilities.CreateInstance<ConsoleSmsSender>(sp);
            return ActivatorUtilities.CreateInstance<HttpSmsSender>(sp);
        });

        return services;
    }
}
