using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Tests.Utils;

public class ProcurementReferenceNumberFormatterTests
{
    [Fact]
    public void AppendSuffixIfNeeded_AddsCurrentYearSuffix_WhenValueHasNoSuffix()
    {
        var result = ProcurementReferenceNumberFormatter.AppendSuffixIfNeeded("SPMP-001");

        Assert.Equal($"SPMP-001/PDC-1110/{DateTime.Now.Year}-S0", result);
    }

    [Fact]
    public void AppendSuffixIfNeeded_DoesNotDuplicateExistingSuffix()
    {
        const string value = "SPMP-001/PDC-1110/2025-S0";

        var result = ProcurementReferenceNumberFormatter.AppendSuffixIfNeeded(value);

        Assert.Equal(value, result);
    }

    [Fact]
    public void RemoveSuffixIfNeeded_RemovesExistingReferenceSuffix()
    {
        var result = ProcurementReferenceNumberFormatter.RemoveSuffixIfNeeded(
            "SPMP-001/PDC-1110/2025-S0"
        );

        Assert.Equal("SPMP-001", result);
    }

    [Fact]
    public void SanitizeFileName_ReturnsFallback_WhenValueIsBlank()
    {
        var result = ProcurementReferenceNumberFormatter.SanitizeFileName("   ");

        Assert.Equal("file", result);
    }
}
