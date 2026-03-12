namespace KeeperData.Core.DeadLetter;

public class RedriveSummary
{
    public int MessagesRedriven { get; set; }
    public int MessagesFailed { get; set; }
    public int MessagesDuplicated { get; set; }
    public int MessagesRemainingApprox { get; set; }
    public List<string> CorrelationIds { get; set; } = [];
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}