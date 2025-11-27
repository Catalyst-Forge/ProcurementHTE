using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ProcurementHTE.Web.Extensions
{
    [HtmlTargetElement("label", Attributes = "asp-for")]
    public class RequiredlabelTagHelper : TagHelper
    {
        public override int Order => 1;

        [HtmlAttributeName("asp-for")]
        public ModelExpression For { get; set; } = default!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (For == null)
                return;

            var metadata = For.Metadata;

            var hasRequiredAttribute = metadata.ValidatorMetadata.OfType<RequiredAttribute>().Any();

            var isRequired = hasRequiredAttribute;

            if (isRequired)
            {
                output.Content.AppendHtml(" <span class=\"text-danger\">*</span>");
            }
        }
    }
}
