namespace KeeperData.Core.DeadLetter;

public readonly struct RedriveResult
{
    public RedriveResultType Type { get; init; }

    public static RedriveResult Success() => new() { Type = RedriveResultType.Success };
    public static RedriveResult Failed() => new() { Type = RedriveResultType.Failed };
    public static RedriveResult Duplicated() => new() { Type = RedriveResultType.Duplicated };
}