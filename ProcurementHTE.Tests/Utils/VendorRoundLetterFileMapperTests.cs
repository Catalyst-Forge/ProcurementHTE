using Microsoft.AspNetCore.Http;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Tests.Utils;

public class VendorRoundLetterFileMapperTests
{
    [Fact]
    public void BuildLookup_MapsNestedVendorLetterFileNames()
    {
        var files = new FormFileCollection
        {
            CreateFile("Vendors[1].LetterFiles[2]"),
            CreateFile("OtherField"),
        };

        var result = VendorRoundLetterFileMapper.BuildLookup(files);

        Assert.True(result.ContainsKey(1));
        Assert.True(result[1].ContainsKey(2));
        Assert.Equal("Vendors[1].LetterFiles[2]", result[1][2].Name);
    }

    [Fact]
    public void Merge_ExpandsBoundFilesAndAppliesUploadedFileByRoundIndex()
    {
        var uploaded = CreateFile("Vendors[0].LetterFiles[2]");
        var lookup = new Dictionary<int, Dictionary<int, IFormFile>>
        {
            [0] = new Dictionary<int, IFormFile> { [2] = uploaded },
        };

        var result = VendorRoundLetterFileMapper.Merge([], lookup, vendorIndex: 0);

        Assert.Equal(3, result.Count);
        Assert.Null(result[0]);
        Assert.Null(result[1]);
        Assert.Same(uploaded, result[2]);
    }

    private static FormFile CreateFile(string fieldName)
    {
        return new FormFile(Stream.Null, 0, 0, fieldName, "letter.pdf");
    }
}
