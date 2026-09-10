using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KeeperData.Api.Controllers.RequestDtos.Holdings;

public class GetHoldingDetailRequest
{
    [FromRoute(Name = "county")]
    [Required]
    [RegularExpression(@"^\d{2}$", ErrorMessage = "County must be a 2-digit number.")]
    public string County { get; set; } = string.Empty;

    [FromRoute(Name = "parish")]
    [Required]
    [RegularExpression(@"^\d{3}$", ErrorMessage = "Parish must be a 3-digit number.")]
    public string Parish { get; set; } = string.Empty;

    [FromRoute(Name = "holding")]
    [Required]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Holding must be a 4-digit number.")]
    public string Holding { get; set; } = string.Empty;
}
