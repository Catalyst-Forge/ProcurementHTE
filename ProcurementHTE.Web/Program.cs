using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Web.Extensions;
using Microsoft.Data.SqlClient;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// === DataProtection Keys ===
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/var/www/ProcurementHTE/keys";
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("ProcurementHTE");

// MVC + app services
builder.Services.AddControllersWithViews();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHttpClient("MinioProxy");

// Session (kamu sudah UseSession di middleware)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".ProcurementHTE.Session";
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

var app = builder.Build();

// Forwarded headers (nginx lokal)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownProxies = { IPAddress.Parse("127.0.0.1") }
});

// --- Hindari nge-log secret di production ---
if (app.Environment.IsDevelopment())
{
    var s = app.Services.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
    app.Logger.LogInformation(
        "ObjectStorage => Endpoint={Endpoint}, SSL={SSL}, Bucket={Bucket}, AccessKey={AK}",
        s.Endpoint, s.UseSSL, s.Bucket, s.AccessKey
    );
}

// ==================== ENSURE TEMPLATES DIRECTORY EXISTS ====================
var envWeb = app.Services.GetRequiredService<IWebHostEnvironment>();

// Pastikan pakai webroot (wwwroot)
var webRoot = envWeb.WebRootPath ?? Path.Combine(envWeb.ContentRootPath, "wwwroot");
var templatesPath = Path.Combine(webRoot, "Templates", "Documents");

// Di server kita TIDAK membuat folder di content root rilisan lagi
Console.WriteLine($"✓ Templates path in use: {templatesPath}");

if (!Directory.Exists(templatesPath))
{
    // Aman membuat di wwwroot kalau memang belum ada
    Directory.CreateDirectory(templatesPath);
    Console.WriteLine($"  Created templates directory.");
}
else
{
    var templates = Directory.GetFiles(templatesPath, "*.html");
    Console.WriteLine(templates.Length > 0
        ? $"  Found {templates.Length} template(s): {string.Join(", ", templates.Select(Path.GetFileName))}"
        : "  ⚠ No templates found. Please add HTML templates.");
}
// ===========================================================================



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
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
        var ctx = services.GetRequiredService<AppDbContext>();
        var skip = string.Equals(Environment.GetEnvironmentVariable("EF_SKIP_MIGRATE"), "1");

        if (!skip)
        {
            await ctx.Database.MigrateAsync();
        }
        await DataSeeder.SeedAsync(services);
    }
    catch (SqlException ex) when (ex.Number == 2714) // object already exists
    {
        // DB sudah ada (mis. tabel Identity), lanjut tanpa gagal
        logger.LogWarning("Skip EF migrate (object already exists): {Message}", ex.Message);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Terjadi kesalahan saat migrasi atau seeding database.");
    }
}

app.Run();
