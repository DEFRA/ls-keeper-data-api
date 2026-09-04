using System.ComponentModel.DataAnnotations;

namespace KeeperData.Api.Controllers.RequestDtos.CphAssociations;

public class GetCphAssociationsRequest
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
}
