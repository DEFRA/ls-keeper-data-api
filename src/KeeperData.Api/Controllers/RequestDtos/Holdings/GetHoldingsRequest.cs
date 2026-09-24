using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KeeperData.Api.Controllers.RequestDtos.Holdings;

public class GetHoldingsRequest
{
    /// <summary>
    /// Page number (1-based). Defaults to 1.
    /// </summary>
    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than or equal to 1.")]
    public int? Page { get; set; } = 1;

    /// <summary>
    /// Number of records per page. Defaults to 10, maximum 100.
    /// </summary>
    [FromQuery(Name = "pageSize")]
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int? PageSize { get; set; } = 10;

    /// <summary>
    /// The sort direction. Available values: asc, desc. Defaults to asc.
    /// </summary>
    [FromQuery(Name = "sort")]
    [RegularExpression("^(?i)(asc|desc)$", ErrorMessage = "Sort must be 'asc' or 'desc'.")]
    public string? Sort { get; set; } = "asc";

    /// <summary>
    /// The field to order the results by: cph, identifier, name, holdingType, startDate, or endDate. Defaults to cph.
    /// </summary>
    [FromQuery(Name = "order")]
    [RegularExpression("^(?i)(cph|identifier|name|holdingType|startDate|endDate)$", ErrorMessage = "Order must be cph, identifier, name, holdingType, startDate, or endDate.")]
    public string? Order { get; set; } = "cph";
}