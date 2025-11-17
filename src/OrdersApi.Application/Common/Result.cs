namespace OrdersApi.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    public ResultErrorType ErrorType { get; private set; }

    public static Result<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value,
        ErrorType = ResultErrorType.None
    };

    public static Result<T> Failure(string error, ResultErrorType errorType) => new()
    {
        IsSuccess = false,
        Error = error,
        ErrorType = errorType
    };
}