using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Web.Extensions;
using ProcurementHTE.Web.Middleware;
using ProcurementHTE.Web.Options;

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
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHttpClient("MinioProxy");
builder.Services.AddHttpClient("SmsProvider");
builder.Services.Configure<SecurityBypassOptions>(
    builder.Configuration.GetSection("SecurityBypass")
);
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

// ===== Migrate & Seed =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // 1️⃣ Jalankan migrasi otomatis jika belum ada tabel
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        await DataSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to migrate or seed database.");
    }
}

app.Run();
