namespace ProductManagement.Core.Common;

/// <summary>
/// Generic Result pattern implementation to cleanly encapsulate service responses,
/// avoiding exception-driven control flow and simplifying unit testing and controller handling.
/// </summary>
/// <typeparam name="T">Payload data type</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? Message { get; }
    public List<string> Errors { get; } = new();

    private Result(bool isSuccess, T? data, string? message, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        Message = message;
        if (errors != null)
        {
            Errors.AddRange(errors);
        }
    }

    public static Result<T> Success(T data, string? message = null)
        => new(true, data, message);

    public static Result<T> Failure(string message, IEnumerable<string>? errors = null)
        => new(false, default, message, errors);

    public static Result<T> Failure(string message, string error)
        => new(false, default, message, new[] { error });
}

/// <summary>
/// Non-generic Result pattern for operations that do not return a data payload.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Message { get; }
    public List<string> Errors { get; } = new();

    private Result(bool isSuccess, string? message, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        if (errors != null)
        {
            Errors.AddRange(errors);
        }
    }

    public static Result Success(string? message = null)
        => new(true, message);

    public static Result Failure(string message, IEnumerable<string>? errors = null)
        => new(false, message, errors);

    public static Result Failure(string message, string error)
        => new(false, message, new[] { error });
}
