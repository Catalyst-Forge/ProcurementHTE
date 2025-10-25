namespace ProcurementHTE.Core.Models.DTOs;

public record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data = default,
    object? Meta = null
)
{
    public static ApiResponse<T> Ok(T data, string message = "OK", object? meta = null)
        => new(true, message, data, meta);

    public static ApiResponse<T> Fail(string message, object? meta = null)
        => new(false, message, default, meta);
}
