namespace KeeperData.Core.DeadLetter;

public readonly struct OperationResult
{
    public bool Success { get; init; }

    public static OperationResult Succeeded() => new() { Success = true };
    public static OperationResult Failed() => new() { Success = false };
}