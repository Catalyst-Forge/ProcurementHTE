using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ProcurementHTE.Web.Extensions;

namespace ProcurementHTE.Web.TagHelpers;

/// <summary>
/// Turns &lt;th sort-key="x"&gt; into the button that opens the column menu (js/column-menu.js).
/// The header stays plain until its column is sorted or filtered.
/// </summary>
[HtmlTargetElement("th", Attributes = KeyAttribute)]
public sealed class SortableHeaderTagHelper : TagHelper
{
    private const string KeyAttribute = "sort-key";

    [HtmlAttributeName(KeyAttribute)]
    public string Key { get; set; } = string.Empty;

    /// <summary>text, date, or number; picks the menu's sort labels.</summary>
    [HtmlAttributeName("sort-kind")]
    public string Kind { get; set; } = "text";

    /// <summary>Adds a value search to the menu, for lists that support it.</summary>
    [HtmlAttributeName("filter-key")]
    public string? FilterKey { get; set; }

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

        var label = await output.GetChildContentAsync();
        var filterValue = FilterKey is null ? null : request.ReadListFilters().GetValueOrDefault(FilterKey);
        var filterAttributes = FilterKey is null
            ? string.Empty
            : $" data-filter-key=\"{Encode(FilterKey)}\" data-filter-value=\"{Encode(filterValue ?? string.Empty)}\"";

        output.Content.Clear();
        output.Content.AppendHtml(
            "<button type=\"button\" class=\"column-header\" aria-haspopup=\"dialog\" aria-expanded=\"false\""
                + $" data-kind=\"{Encode(Kind)}\""
                + $" data-sort-asc=\"{Encode(request.WithSort(Key, descending: false))}\""
                + $" data-sort-desc=\"{Encode(request.WithSort(Key, descending: true))}\""
                + (isActive ? $" data-sorted=\"{(isDescending ? "desc" : "asc")}\"" : string.Empty)
                + filterAttributes
                + ">"
        );
        output.Content.AppendHtml("<span class=\"column-header-label\">");
        output.Content.AppendHtml(label);
        output.Content.AppendHtml("</span>");
        if (isActive)
        {
            var icon = isDescending ? "bi-caret-down-fill" : "bi-caret-up-fill";
            output.Content.AppendHtml($"<i class=\"bi {icon} column-sort-mark\" aria-hidden=\"true\"></i>");
        }
        if (!string.IsNullOrEmpty(filterValue))
            output.Content.AppendHtml("<span class=\"column-filter-mark\" aria-hidden=\"true\"></span>");
        output.Content.AppendHtml("</button>");
    }

    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
}
