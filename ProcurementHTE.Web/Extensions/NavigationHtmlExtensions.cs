using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProcurementHTE.Web.Extensions
{
    public static class NavigationHtmlExtensions
    {
        public static bool IsRouteActive(
            this IHtmlHelper htmlHelper,
            IEnumerable<string>? controllers = null,
            IEnumerable<string>? actions = null,
            string? area = null,
            string? pathStartsWith = null
        )
        {
            var viewContext = htmlHelper.ViewContext;
            var routeData = viewContext.RouteData;

            routeData.Values.TryGetValue("area", out var areaValue);
            routeData.Values.TryGetValue("controller", out var controllerValue);
            routeData.Values.TryGetValue("action", out var actionValue);

            var currentArea = areaValue?.ToString();
            var currentController = controllerValue?.ToString();
            var currentAction = actionValue?.ToString();
            var currentPath = viewContext.HttpContext?.Request?.Path.Value ?? string.Empty;

            var areaMatches =
                string.IsNullOrEmpty(area)
                || string.Equals(currentArea, area, StringComparison.OrdinalIgnoreCase);

            var controllerMatches =
                controllers is null || !controllers.Any()
                    ? true
                    : controllers.Any(c =>
                        string.Equals(c, currentController, StringComparison.OrdinalIgnoreCase)
                    );

            var actionMatches =
                actions is null || !actions.Any()
                    ? true
                    : actions.Any(a =>
                        string.Equals(a, currentAction, StringComparison.OrdinalIgnoreCase)
                    );

            var pathMatches =
                string.IsNullOrEmpty(pathStartsWith)
                || (
                    !string.IsNullOrEmpty(currentPath)
                    && currentPath.StartsWith(pathStartsWith, StringComparison.OrdinalIgnoreCase)
                );

            return areaMatches && controllerMatches && actionMatches && pathMatches;
        }

        public static string ActiveClass(
            this IHtmlHelper htmlHelper,
            string className = "active",
            IEnumerable<string>? controllers = null,
            IEnumerable<string>? actions = null,
            string? area = null,
            string? pathStartsWith = null
        )
        {
            return htmlHelper.IsRouteActive(controllers, actions, area, pathStartsWith)
                ? className
                : string.Empty;
        }

        public static string CollapseState(
            this IHtmlHelper htmlHelper,
            string className = "show",
            IEnumerable<string>? controllers = null,
            IEnumerable<string>? actions = null,
            string? area = null,
            string? pathStartsWith = null
        )
        {
            return htmlHelper.IsRouteActive(controllers, actions, area, pathStartsWith)
                ? className
                : string.Empty;
        }
    }
}
