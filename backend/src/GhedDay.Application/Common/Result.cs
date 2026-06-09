namespace GhedDay.Application.Common;

/// <summary>Lightweight success/failure result to avoid exceptions for expected flow outcomes.</summary>
public readonly record struct Result
{
    public bool Succeeded { get; private init; }
    public string? Error { get; private init; }

    public static Result Success() => new() { Succeeded = true };
    public static Result Failure(string error) => new() { Succeeded = false, Error = error };
}

public readonly record struct Result<T>
{
    public bool Succeeded { get; private init; }
    public T? Value { get; private init; }
    public string? Error { get; private init; }

    public static Result<T> Success(T value) => new() { Succeeded = true, Value = value };
    public static Result<T> Failure(string error) => new() { Succeeded = false, Error = error };
}
