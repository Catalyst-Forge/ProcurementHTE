using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Tests.Utils;

public class IndonesianPhoneNumberFormatterTests
{
    [Theory]
    [InlineData("08123456789", "+628123456789")]
    [InlineData("628123456789", "+628123456789")]
    [InlineData("+62 812-3456-789", "+628123456789")]
    public void NormalizeForStorage_ReturnsInternationalIndonesianFormat(
        string input,
        string expected
    )
    {
        var result = IndonesianPhoneNumberFormatter.NormalizeForStorage(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeForStorage_ReturnsNull_WhenNoDigitsProvided()
    {
        var result = IndonesianPhoneNumberFormatter.NormalizeForStorage("abc");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("+628123456789", "8123456789")]
    [InlineData("628123456789", "8123456789")]
    [InlineData("08123456789", "8123456789")]
    public void FormatForInput_RemovesCountryAndLeadingZeroPrefix(string input, string expected)
    {
        var result = IndonesianPhoneNumberFormatter.FormatForInput(input);

        Assert.Equal(expected, result);
    }
}
