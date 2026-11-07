using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ProcurementHTE.Web.Extensions;

namespace ProcurementHTE.Web.TagHelpers;

/// <summary>
/// Turns &lt;th sort-key="x"&gt; into a link that sorts the list on the server. The first click
/// sorts ascending and clicking the same column again reverses it.
/// </summary>
[HtmlTargetElement("th", Attributes = KeyAttribute)]
public sealed class SortableHeaderTagHelper : TagHelper
{
    private const string KeyAttribute = "sort-key";

    [HtmlAttributeName(KeyAttribute)]
    public string Key { get; set; } = string.Empty;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var request = ViewContext.HttpContext.Request;
        var sort = request.ReadListSort();
        var isActive = string.Equals(sort?.Key, Key, StringComparison.OrdinalIgnoreCase);
        var isDescending = isActive && sort!.Descending;

        if (isActive)
            output.Attributes.SetAttribute("aria-sort", isDescending ? "descending" : "ascending");

        var icon = !isActive ? "bi-chevron-expand" : isDescending ? "bi-caret-down-fill" : "bi-caret-up-fill";
        var url = HtmlEncoder.Default.Encode(request.WithSort(Key, descending: isActive && !isDescending));
        var label = await output.GetChildContentAsync();

        output.Content.Clear();
        output.Content.AppendHtml($"<a class=\"sort-link\" href=\"{url}\">");
        output.Content.AppendHtml(label);
        output.Content.AppendHtml($"<i class=\"bi {icon} sort-icon\" aria-hidden=\"true\"></i></a>");
    }
}
