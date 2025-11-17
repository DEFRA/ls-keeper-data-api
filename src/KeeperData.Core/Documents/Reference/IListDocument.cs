namespace KeeperData.Core.Documents.Reference;

public interface IListDocument
{
    string Id { get; set; }
    DateTime LastUpdatedDate { get; set; }
}