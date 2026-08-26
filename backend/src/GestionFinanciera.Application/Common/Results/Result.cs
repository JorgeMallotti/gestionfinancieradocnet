namespace GestionFinanciera.Application.Common.Results;

/// <summary>
/// Result pattern — represents the outcome of an operation without using exceptions
/// for expected failures. Services return <c>Result</c>/<c>Result&lt;T&gt;</c> instead of throwing.
/// </summary>
public readonly record struct Result
{
    public bool IsSuccess { get; init; }

    public bool IsFailure => !IsSuccess;

    /// <summary>Typed error code (see <see cref="ErrorCode"/>). Only meaningful on failure.</summary>
    public ErrorCode Code { get; init; }

    public string? Error { get; init; }

    public static Result Success() => new() { IsSuccess = true };

    public static Result Failure(ErrorCode code, string error) =>
        new() { IsSuccess = false, Code = code, Error = error };

    /// <summary>Failure with the default <see cref="ErrorCode.Validation"/> code.</summary>
    public static Result Failure(string error) => Failure(ErrorCode.Validation, error);
}

/// <summary>Result pattern with a payload.</summary>
public readonly record struct Result<T>
{
    public bool IsSuccess { get; init; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; init; }

    /// <summary>Typed error code (see <see cref="ErrorCode"/>). Only meaningful on failure.</summary>
    public ErrorCode Code { get; init; }

    public string? Error { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };

    public static Result<T> Failure(ErrorCode code, string error) =>
        new() { IsSuccess = false, Code = code, Error = error };

    /// <summary>Failure with the default <see cref="ErrorCode.Validation"/> code.</summary>
    public static Result<T> Failure(string error) => Failure(ErrorCode.Validation, error);
}
