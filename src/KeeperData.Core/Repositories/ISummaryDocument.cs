namespace KeeperData.Core.Repositories
{
    public interface ISummaryDocument
    {
        string IdentifierId { get; set; }
        string Code { get; set; }
        string Name { get; set; }
        DateTime? LastUpdatedDate { get; set; }
    }
}