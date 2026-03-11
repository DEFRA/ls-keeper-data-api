namespace KeeperData.Core.DeadLetter;

public class DeadLetterMessagesResult
{
    public List<DeadLetterMessageDto> Messages { get; set; } = new();
    public int TotalApproximateCount { get; set; }
    public DateTime CheckedAt { get; set; }
}