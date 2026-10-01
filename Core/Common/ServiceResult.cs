using System.ComponentModel;

namespace Core.Common;

public enum ServiceErrorType
{
    Validation,
    NotFound,
    Conflict    
}

public sealed class ServiceResult<T>
{
    public bool Succeeded { get; private init; }
    public T? Value { get; private init; }
    public string? Error { get; private init; }
    public ServiceErrorType ErrorType { get; private init; }

    public static ServiceResult<T> Success(T value) => new() { Succeeded = true, Value = value };
    public static ServiceResult<T> Fail(string error, ServiceErrorType type = ServiceErrorType.Validation) =>
        new() { Succeeded = false, Error = error, ErrorType = type };
}