using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Core.Services
{
    public class FileSystemTemplateProvider : ITemplateProvider
    {
        private readonly string _templatesPath;

        public FileSystemTemplateProvider()
        {
            // Templates disimpan di folder Templates/Documents dari root project
            _templatesPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "Documents"
            );

            // Buat folder jika belum ada
            if (!Directory.Exists(_templatesPath))
            {
                Directory.CreateDirectory(_templatesPath);
            }
        }

        public async Task<string> GetTemplateAsync(
            string templateName,
            CancellationToken ct = default
        )
        {
            // Sanitize template name (remove any path traversal attempts)
            var safeName = Path.GetFileName(templateName);
            var filePath = Path.Combine(_templatesPath, $"{safeName}.html");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    $"Template '{templateName}' tidak ditemukan di {filePath}"
                );
            }

            try
            {
                return await File.ReadAllTextAsync(filePath, ct);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Gagal membaca template '{templateName}': {ex.Message}",
                    ex
                );
            }
        }
    }
}
