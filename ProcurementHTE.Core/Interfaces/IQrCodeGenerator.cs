namespace ProcurementHTE.Core.Interfaces;

/// <summary>
/// Service for generating QR codes locally without external API dependencies.
/// </summary>
public interface IQrCodeGenerator
{
    /// <summary>
    /// Generates a QR code as a base64 data URI (data:image/png;base64,...).
    /// </summary>
    /// <param name="content">The content to encode in the QR code.</param>
    /// <param name="pixelsPerModule">Size of each module/pixel in the QR code (default: 10).</param>
    /// <returns>A base64 data URI string that can be used directly in img src.</returns>
    string GenerateAsDataUri(string content, int pixelsPerModule = 10);

    /// <summary>
    /// Generates a QR code as PNG bytes.
    /// </summary>
    /// <param name="content">The content to encode in the QR code.</param>
    /// <param name="pixelsPerModule">Size of each module/pixel in the QR code (default: 10).</param>
    /// <returns>PNG image bytes.</returns>
    byte[] GenerateAsPng(string content, int pixelsPerModule = 10);
}
