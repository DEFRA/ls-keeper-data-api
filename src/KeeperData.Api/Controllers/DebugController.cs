using KeeperData.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly ICountryIdentifierLookupService _countryLookupService;

    public DebugController(ICountryIdentifierLookupService countryLookupService)
    {
        _countryLookupService = countryLookupService;
    }

    [HttpGet("country/{lookupValue}")]
    public async Task<IActionResult> LookupCountry(string lookupValue, CancellationToken cancellationToken)
    {
        var result = await _countryLookupService.FindAsync(lookupValue, cancellationToken);

        if (result.countryId == null)
        {
            return NotFound(new { message = $"Country not found for: {lookupValue}" });
        }

        return Ok(new
        {
            lookupValue,
            countryId = result.countryId,
            countryName = result.countryName
        });
    }
}
