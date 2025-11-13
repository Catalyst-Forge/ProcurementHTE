using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Core.Services
{
    public class FileSystemTemplateProvider : ITemplateProvider
    {
        private readonly string _templatesPath;
        private readonly ILogger<FileSystemTemplateProvider> _logger;

        public FileSystemTemplateProvider(ILogger<FileSystemTemplateProvider> logger)
        {
            _logger = logger;

            // Templates disimpan di folder Templates/Documents dari root project
            _templatesPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "Documents"
            );

            _logger.LogInformation("Template provider initialized. Path: {Path}", _templatesPath);

            // Buat folder jika belum ada
            if (!Directory.Exists(_templatesPath))
            {
                Directory.CreateDirectory(_templatesPath);
                _logger.LogWarning("Templates directory created at: {Path}", _templatesPath);
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

            _logger.LogDebug(
                "Loading template: {TemplateName} from {Path}",
                templateName,
                filePath
            );

            if (!File.Exists(filePath))
            {
                _logger.LogError("Template not found: {Path}", filePath);
                throw new FileNotFoundException(
                    $"Template '{templateName}' tidak ditemukan di {filePath}"
                );
            }

            try
            {
                var content = await File.ReadAllTextAsync(filePath, ct);
                _logger.LogDebug(
                    "Template loaded successfully: {TemplateName}, Size: {Size} chars",
                    templateName,
                    content.Length
                );
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error reading template: {TemplateName} from {Path}",
                    templateName,
                    filePath
                );
                throw new InvalidOperationException(
                    $"Gagal membaca template '{templateName}': {ex.Message}",
                    ex
                );
            }
        }
    }
}
