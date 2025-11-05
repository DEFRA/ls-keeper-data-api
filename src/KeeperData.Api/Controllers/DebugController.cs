using KeeperData.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly ICountryIdentifierLookupService _countryLookupService;
    private readonly IWebHostEnvironment _env;

    public DebugController(
        ICountryIdentifierLookupService countryLookupService,
        IWebHostEnvironment env)
    {
        _countryLookupService = countryLookupService;
        _env = env;
    }

    [HttpGet("country/{lookupValue}")]
    public async Task<IActionResult> LookupCountry(string lookupValue, CancellationToken cancellationToken)
    {
        // Arrange
        var result = await _countryLookupService.FindAsync(lookupValue, cancellationToken);

        // Act
        if (result.countryId == null)
        {
            return NotFound(new { message = $"Country not found for: {lookupValue}" });
        }

        // Assert
        return Ok(new
        {
            lookupValue,
            countryId = result.countryId,
            countryName = result.countryName
        });
    }

    [HttpGet("filesystem")]
    public IActionResult ListFilesystem()
    {
        var result = new
        {
            ContentRootPath = _env.ContentRootPath,
            WebRootPath = _env.WebRootPath,
            EnvironmentName = _env.EnvironmentName,
            ApplicationName = _env.ApplicationName,
            Files = GetFilesRecursive(_env.ContentRootPath, _env.ContentRootPath)
        };

        return Ok(result);
    }

    private List<string> GetFilesRecursive(string rootPath, string currentPath, int maxDepth = 5, int currentDepth = 0)
    {
        var files = new List<string>();

        if (currentDepth >= maxDepth || !Directory.Exists(currentPath))
            return files;

        try
        {
            // Add all files in current directory
            foreach (var file in Directory.GetFiles(currentPath))
            {
                files.Add(file.Replace(rootPath, "").TrimStart('/'));
            }

            // Recursively add files from subdirectories
            foreach (var dir in Directory.GetDirectories(currentPath))
            {
                files.AddRange(GetFilesRecursive(rootPath, dir, maxDepth, currentDepth + 1));
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }

        return files;
    }
}