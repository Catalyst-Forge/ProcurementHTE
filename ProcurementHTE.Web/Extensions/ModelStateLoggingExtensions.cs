using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace ProcurementHTE.Web.Extensions;

public static class ModelStateLoggingExtensions
{
    public static void LogInvalidModelState(
        this ILogger logger,
        ModelStateDictionary modelState,
        string area,
        string context,
        string traceId
    )
    {
        if (modelState.IsValid)
            return;

        var errors = modelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .Select(kvp =>
                $"{kvp.Key}: {string.Join(" | ", kvp.Value!.Errors.Select(e => e.ErrorMessage ?? e.Exception?.Message ?? "<no message>"))}"
            )
            .ToArray();

        if (errors.Length == 0)
            return;

        logger.LogDebug(
            "ModelState invalid for {Area} at {Context}. TraceId={TraceId}. Errors={Errors}",
            area,
            context,
            traceId,
            string.Join("; ", errors)
        );
    }
}
