using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Options;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Web.Extensions;
using ProcurementHTE.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// === DataProtection Keys ===
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/var/www/ProcurementHTE/keys";
Directory.CreateDirectory(keysPath);
builder
    .Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("ProcurementHTE");

// MVC + app services
builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationLayer();
builder.Services.AddIdentityAndAuth(builder.Configuration);
builder.Services.AddPresentationLayer();
builder.Services.AddHttpClient("MinioProxy");
builder.Services.Configure<SecurityBypassOptions>(
    builder.Configuration.GetSection("SecurityBypass")
);

// SignalR for real-time updates
builder.Services.AddSignalR();
builder.Services.AddSingleton<
    ProcurementHTE.Core.Interfaces.IUserActivityNotifier,
    ProcurementHTE.Infrastructure.Services.UserActivityNotifier<ProcurementHTE.Web.Hubs.DashboardHub>
>();
builder.Services.AddSingleton<
    ProcurementHTE.Core.Interfaces.INotificationPusher,
    ProcurementHTE.Infrastructure.Services.NotificationPusher<ProcurementHTE.Web.Hubs.DashboardHub>
>();
var app = builder.Build();

// ==================== ENSURE TEMPLATES DIRECTORY EXISTS ====================
var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Documents");
if (!Directory.Exists(templatesPath))
{
    Directory.CreateDirectory(templatesPath);
}

// ===========================================================================

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error/500");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<UserSessionValidationMiddleware>();
app.UseMiddleware<SecurityCheckpointMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Map SignalR hub
app.MapHub<ProcurementHTE.Web.Hubs.DashboardHub>("/hubs/dashboard");

// ===== Migrate & Seed =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();

    try
    {
        // 1️⃣ Cek apakah ada pending migrations
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            logger.LogInformation(
                "Applying {Count} pending migration(s)...",
                pendingMigrations.Count()
            );
            foreach (var migration in pendingMigrations)
            {
                logger.LogInformation("  - {Migration}", migration);
            }

            await context.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date. No pending migrations.");
        }

        // 2️⃣ Jalankan seeder
        await DataSeeder.SeedAsync(services);
        logger.LogInformation("Data seeding completed.");
    }
    catch (Exception ex)
        when (ex.Message.Contains("already exists")
            || ex.Message.Contains("already an object named")
            || ex.Message.Contains("duplicate key")
        )
    {
        // Skip jika table/column sudah ada
        logger.LogWarning(
            "Migration skipped - database objects already exist: {Message}",
            ex.Message
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to migrate or seed database");
        throw;
    }
}

app.Run();
