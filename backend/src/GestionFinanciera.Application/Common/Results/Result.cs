namespace GestionFinanciera.Application.Common.Results;

/// <summary>
/// Result pattern — represents the outcome of an operation without using exceptions
/// for expected failures. Services return <c>Result</c>/<c>Result&lt;T&gt;</c> instead of throwing.
/// </summary>
public readonly record struct Result
{
    public bool IsSuccess { get; init; }

    public bool IsFailure => !IsSuccess;

    public string? Error { get; init; }

    public static Result Success() => new() { IsSuccess = true };

    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}

/// <summary>Result pattern with a payload.</summary>
public readonly record struct Result<T>
{
    public bool IsSuccess { get; init; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; init; }

    public string? Error { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };

    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
