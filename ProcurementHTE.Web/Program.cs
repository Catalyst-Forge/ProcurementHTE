using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Infrastructure.Storage;
using ProcurementHTE.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHttpClient("MinioProxy");
var app = builder.Build();

// Nanti Hapus ini setelah yakin konfigurasi Object Storage benar
var s = app.Services.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
app.Logger.LogInformation(
    "ObjectStorage => Endpoint={Endpoint}, SSL={SSL}, Bucket={Bucket}, AccessKey={AK}, SecretKey={SecretKey}",
    s.Endpoint,
    s.UseSSL,
    s.Bucket,
    s.AccessKey,
    s.SecretKey
);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.Use(
    async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            Console.WriteLine($"======================================");
            Console.WriteLine($"[API Request] {context.Request.Method} {context.Request.Path}");
            Console.WriteLine(
                $"[Authorization Header] {(string.IsNullOrEmpty(authHeader) ? "MISSING" : $"Present - {authHeader.Substring(0, Math.Min(30, authHeader.Length))}...")}"
            );
            Console.WriteLine($"[All Headers]:");
            foreach (var header in context.Request.Headers)
            {
                Console.WriteLine($"  {header.Key}: {header.Value}");
            }
        }

        await next();

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            Console.WriteLine($"[Response] Status: {context.Response.StatusCode}");
            Console.WriteLine($"[User Authenticated] {context.User.Identity?.IsAuthenticated}");
            Console.WriteLine($"[Auth Type] {context.User.Identity?.AuthenticationType ?? "None"}");
            Console.WriteLine($"======================================");
        }
    }
);

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(name: "default", pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // 1️⃣ Jalankan migrasi otomatis jika belum ada tabel
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        // 2️⃣ Baru jalankan seeding
        await DataSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Terjadi kesalahan saat migrasi atau seeding database.");
    }
}

app.Run();
