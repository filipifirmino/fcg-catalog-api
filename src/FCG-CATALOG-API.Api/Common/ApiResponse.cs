namespace FCG_CATALOG_API.Api.Common;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    private ApiResponse(bool success, T? data, IEnumerable<string> errors)
    {
        Success = success;
        Data = data;
        Errors = errors.ToList();
    }

    public static ApiResponse<T> SuccessResponse(T data) =>
        new(true, data, Array.Empty<string>());

    public static ApiResponse<T> ErrorResponse(IEnumerable<string> errors) =>
        new(false, default, errors);

    public static ApiResponse<T> ErrorResponse(string error) =>
        new(false, default, new[] { error });
}
