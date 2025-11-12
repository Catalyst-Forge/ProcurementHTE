using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.DesignTime
{
    /// <summary>
    /// Dipakai EF Tools saat design-time (dotnet ef *) untuk membuat AppDbContext
    /// tanpa harus membangun ASP.NET host (jadi aman meski Jwt/ObjectStorage tidak ada).
    /// Prioritas sumber koneksi:
    /// 1) ENV: EF_CONNECTION
    /// 2) arg: --connection "<connstring>"
    /// 3) appsettings*.json (optional, fallback)
    /// </summary>
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString = ReadConnectionString(args);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "No connection string for design-time. " +
                    "Set ENV EF_CONNECTION atau lewat arg --connection \"<connstring>\", " +
                    "atau sediakan appsettings.json dengan ConnectionStrings:DefaultConnection."
                );

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                })
                .Options;

            return new AppDbContext(options);
        }

        private static string? ReadConnectionString(string[] args)
        {
            // 1) ENV terlebih dulu
            var env = Environment.GetEnvironmentVariable("EF_CONNECTION");
            if (!string.IsNullOrWhiteSpace(env)) return env;

            // 2) Arg sederhana: --connection "<conn>"
            var dict = ParseArgs(args);
            if (dict.TryGetValue("--connection", out var argConn) && !string.IsNullOrWhiteSpace(argConn))
                return argConn;

            // 3) Fallback: appsettings* di working dir (tidak wajib)
            var basePath = Directory.GetCurrentDirectory();
            var cfg = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables() // ConnectionStrings__DefaultConnection juga bisa
                .Build();

            return cfg.GetConnectionString("DefaultConnection");
        }

        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length; i++)
            {
                var key = args[i];
                if (key.StartsWith("--", StringComparison.Ordinal))
                {
                    var val = (i + 1 < args.Length) ? args[i + 1] : "";
                    d[key] = val;
                    i++;
                }
            }
            return d;
        }
    }
}
