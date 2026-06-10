using ProcurementHTE.Core.Interfaces;
using QRCoder;

namespace ProcurementHTE.Infrastructure.Services;

/// <summary>
/// Local QR code generator using QRCoder library.
/// Cross-platform compatible (Windows, Linux, macOS) - no external API dependencies.
/// </summary>
public class QrCodeGeneratorService : IQrCodeGenerator
{
    /// <inheritdoc />
    public string GenerateAsDataUri(string content, int pixelsPerModule = 10)
    {
        var pngBytes = GenerateAsPng(content, pixelsPerModule);
        var base64 = Convert.ToBase64String(pngBytes);
        return $"data:image/png;base64,{base64}";
    }

    /// <inheritdoc />
    public byte[] GenerateAsPng(string content, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(pixelsPerModule);
    }
}
