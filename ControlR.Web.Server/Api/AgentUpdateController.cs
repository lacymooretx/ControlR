using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using ControlR.Libraries.Shared.Constants;
using Microsoft.AspNetCore.OutputCaching;

namespace ControlR.Web.Server.Api;

[ApiController]
[Route(HttpConstants.AgentUpdateEndpoint)]
public class AgentUpdateController(
  ILogger<AgentUpdateController> logger,
  IWebHostEnvironment webHostEnvironment) : ControllerBase
{
  private const int CacheDurationSeconds = 600;

  private readonly ILogger<AgentUpdateController> _logger = logger;
  private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

  [OutputCache(Duration = CacheDurationSeconds)]
  [ResponseCache(Duration = CacheDurationSeconds, Location = ResponseCacheLocation.Any)]
  [Produces("text/plain")]
  [HttpGet("get-hash-sha256/{runtime}")]
  public async Task<ActionResult<string>> GetHash(RuntimeId runtime, CancellationToken cancellationToken)
  {
    var relativePath = AppConstants.GetAgentFileDownloadPath(runtime);
    _logger.LogDebug("GetHash request started for downloads file. Path: {FilePath}", relativePath);

    var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath.TrimStart('/'));
    if (!System.IO.File.Exists(physicalPath))
    {
      _logger.LogWarning("File does not exist: {FilePath}", physicalPath);
      return NotFound();
    }

    _logger.LogDebug("Calculating hash.");
    await using var fs = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    var sha256Hash = await SHA256.HashDataAsync(fs, cancellationToken);
    var hexHash = Convert.ToHexString(sha256Hash);

    return Ok(hexHash);
  }
}