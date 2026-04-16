namespace KeeperData.Core.Results;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public string? ErrorCode { get; }

    private Result(bool isSuccess, T? value, string? errorMessage, string? errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string errorMessage, string? errorCode = null) => 
        new(false, default, errorMessage, errorCode);

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess 
            ? Result<TNew>.Success(mapper(Value!)) 
            : Result<TNew>.Failure(ErrorMessage!, ErrorCode);
    }

    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
    {
        return IsSuccess 
            ? binder(Value!) 
            : Result<TNew>.Failure(ErrorMessage!, ErrorCode);
    }
}