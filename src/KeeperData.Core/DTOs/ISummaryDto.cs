namespace KeeperData.Core.DTOs;

public interface ISummaryDto
{
    string IdentifierId { get; set; }
    string Code { get; set; }
    string Name { get; set; }
    DateTime? LastUpdatedDate { get; set; }
}