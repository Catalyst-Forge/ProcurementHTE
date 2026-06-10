using System;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Services;

public partial class DocumentGenerator : IDocumentGenerator
{
    private readonly ITemplateProvider _templateProvider;
    private readonly IHtmlTokenReplacer _tokenReplacer;
    private readonly IProfitLossRepository _pnlRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly string _logoUrl;
    private readonly string _footerUrl;

    public DocumentGenerator(
        ITemplateProvider templateProvider,
        IHtmlTokenReplacer tokenReplacer,
        IProfitLossRepository pnlRepository,
        IVendorRepository vendorRepository
    )
    {
        _templateProvider = templateProvider;
        _tokenReplacer = tokenReplacer;
        _pnlRepository = pnlRepository;
        _vendorRepository = vendorRepository;

        _logoUrl = BuildImageDataUri(Path.Combine("wwwroot", "images", "logo.png"));
        _footerUrl = BuildImageDataUri(Path.Combine("wwwroot", "images", "footer_document.png"));
    }

    public async Task<byte[]> GenerateFromTemplateAsync(
        string templateName,
        object model,
        CancellationToken ct = default
    )
    {
        var template = await _templateProvider.GetTemplateAsync(templateName, ct);
        string html;

        if (model is Procurement ProcurementModel)
        {
            html = await _tokenReplacer.ReplaceTokensAsync(
                template,
                ProcurementModel,
                ct,
                templateName
            );
        }
        else
        {
            html = template;
            var type = model.GetType();

            foreach (var prop in type.GetProperties())
            {
                var value = prop.GetValue(model)?.ToString() ?? string.Empty;
                html = html.Replace($"{{{{{prop.Name}}}}}", value);
            }
        }

        html = ApplyCommonTokens(html);

        return await HtmlToPdfAsync(html, templateName, ct);
    }

    #region Utilities

    private async Task<byte[]> GenerateByTemplateAsync(
        string templateKey,
        string title,
        Procurement procurement,
        CancellationToken ct
    )
    {
        var template = await _templateProvider.GetTemplateAsync(templateKey, ct);
        var html = await _tokenReplacer.ReplaceTokensAsync(template, procurement, ct, templateKey);

        html = ApplyCommonTokens(html);

        return await HtmlToPdfAsync(html, title, ct);
    }

    private static async Task<byte[]> HtmlToPdfAsync(
        string html,
        string title,
        CancellationToken ct
    )
    {
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true }
        );
        var page = await browser.NewPageAsync();

        await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.NetworkIdle });

        var pdf = await page.PdfAsync(
            new()
            {
                Format = "A4",
                PrintBackground = true,
                DisplayHeaderFooter = false,
                Margin = new()
                {
                    Top = "12mm",
                    Right = "12mm",
                    Bottom = "14mm",
                    Left = "12mm",
                },
                HeaderTemplate =
                    $"<div style=\"font-size: 10px; width: 100%; text-align: right; padding-right: 8px;\">{System.Net.WebUtility.HtmlEncode(title)}</div>",
                FooterTemplate =
                    "<div style='font-size: 10px; width: 100%; text-align: right; padding-right: 8px;'>Hal <span class=\"pageNumber\"></span><span class=\"totalPages\"></span></div>",
            }
        );

        return pdf;
    }

    private string ApplyCommonTokens(string html)
    {
        return html.Replace("{{LogoUrl}}", _logoUrl).Replace("{{FooterUrl}}", _footerUrl);
    }

    private static string BuildImageDataUri(string relativePath)
    {
        var contentRoot = Directory.GetCurrentDirectory();
        var fullPath = Path.Combine(contentRoot, relativePath);

        fullPath = Path.GetFullPath(fullPath);

        if (!File.Exists(fullPath))
        {
            return string.Empty;
        }

        var bytes = File.ReadAllBytes(fullPath);
        var base64 = Convert.ToBase64String(bytes);

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var mime = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "image/png",
        };

        return $"data:{mime};base64,{base64}";
    }

    #endregion
}
