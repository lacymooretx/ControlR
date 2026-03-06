using System.Security.Claims;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/branding")]
[ApiController]
public class BrandingController : ControllerBase
{
  [HttpGet]
  [AllowAnonymous]
  public async Task<ActionResult<BrandingSettingsDto>> GetBranding(
    [FromServices] IDbContextFactory<AppDb> dbFactory)
  {
    await using var appDb = await dbFactory.CreateDbContextAsync();
    var settings = await appDb.BrandingSettings
      .AsNoTracking()
      .FirstOrDefaultAsync();

    if (settings is null)
    {
      return Ok(new BrandingSettingsDto(
        Guid.Empty,
        "ControlR",
        "#2196F3",
        null,
        null,
        false));
    }

    return Ok(settings.ToDto());
  }

  [HttpPut]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<BrandingSettingsDto>> UpdateBranding(
    [FromServices] AppDb appDb,
    [FromBody] UpdateBrandingRequestDto request)
  {
    var tenantId = User.FindFirstValue("TenantId");
    if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
    {
      return BadRequest("Invalid tenant.");
    }

    if (string.IsNullOrWhiteSpace(request.ProductName) || request.ProductName.Length > 100)
    {
      return BadRequest("Product name is required and must be 100 characters or fewer.");
    }

    if (string.IsNullOrWhiteSpace(request.PrimaryColor) || request.PrimaryColor.Length > 20)
    {
      return BadRequest("Primary color is required and must be 20 characters or fewer.");
    }

    var settings = await appDb.BrandingSettings.FirstOrDefaultAsync();

    if (settings is null)
    {
      settings = new BrandingSettings
      {
        TenantId = tenantGuid,
        ProductName = request.ProductName,
        PrimaryColor = request.PrimaryColor,
        SecondaryColor = request.SecondaryColor
      };
      await appDb.BrandingSettings.AddAsync(settings);
    }
    else
    {
      settings.ProductName = request.ProductName;
      settings.PrimaryColor = request.PrimaryColor;
      settings.SecondaryColor = request.SecondaryColor;
    }

    await appDb.SaveChangesAsync();
    return Ok(settings.ToDto());
  }

  [HttpPost("logo")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  [RequestSizeLimit(5 * 1024 * 1024)]
  public async Task<ActionResult<BrandingSettingsDto>> UploadLogo(
    [FromServices] AppDb appDb,
    [FromServices] IOptions<AppOptions> appOptions)
  {
    var tenantId = User.FindFirstValue("TenantId");
    if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
    {
      return BadRequest("Invalid tenant.");
    }

    if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
    {
      return BadRequest("No file uploaded.");
    }

    var file = Request.Form.Files[0];

    if (file.Length > 5 * 1024 * 1024)
    {
      return BadRequest("File size exceeds 5MB limit.");
    }

    var contentType = file.ContentType.ToLowerInvariant();
    if (!contentType.StartsWith("image/"))
    {
      return BadRequest("Only image files are allowed.");
    }

    var settings = await appDb.BrandingSettings.FirstOrDefaultAsync();

    if (settings is null)
    {
      settings = new BrandingSettings
      {
        TenantId = tenantGuid
      };
      await appDb.BrandingSettings.AddAsync(settings);
      await appDb.SaveChangesAsync();
    }

    // Delete existing logo if present
    if (!string.IsNullOrEmpty(settings.LogoStoragePath) &&
        System.IO.File.Exists(Path.Combine(settings.LogoStoragePath, settings.LogoFileName ?? "")))
    {
      try
      {
        Directory.Delete(settings.LogoStoragePath, recursive: true);
      }
      catch
      {
        // Log but don't fail
      }
    }

    var storagePath = Path.Combine(
      appOptions.Value.BrandingStoragePath,
      tenantGuid.ToString(),
      settings.Id.ToString());

    Directory.CreateDirectory(storagePath);

    var filePath = Path.Combine(storagePath, file.FileName);
    await using (var stream = new FileStream(filePath, FileMode.Create))
    {
      await file.CopyToAsync(stream);
    }

    settings.LogoFileName = file.FileName;
    settings.LogoStoragePath = storagePath;

    await appDb.SaveChangesAsync();
    return Ok(settings.ToDto());
  }

  [HttpGet("logo")]
  [AllowAnonymous]
  public async Task<ActionResult> GetLogo(
    [FromServices] IDbContextFactory<AppDb> dbFactory)
  {
    await using var appDb = await dbFactory.CreateDbContextAsync();
    var settings = await appDb.BrandingSettings
      .AsNoTracking()
      .FirstOrDefaultAsync();

    if (settings is null ||
        string.IsNullOrEmpty(settings.LogoStoragePath) ||
        string.IsNullOrEmpty(settings.LogoFileName))
    {
      return NotFound();
    }

    var filePath = Path.Combine(settings.LogoStoragePath, settings.LogoFileName);
    if (!System.IO.File.Exists(filePath))
    {
      return NotFound("Logo file not found on disk.");
    }

    var extension = Path.GetExtension(settings.LogoFileName).ToLowerInvariant();
    var mimeType = extension switch
    {
      ".png" => "image/png",
      ".jpg" or ".jpeg" => "image/jpeg",
      ".gif" => "image/gif",
      ".svg" => "image/svg+xml",
      ".webp" => "image/webp",
      ".ico" => "image/x-icon",
      _ => "application/octet-stream"
    };

    var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    return File(stream, mimeType, settings.LogoFileName);
  }

  [HttpDelete("logo")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<BrandingSettingsDto>> DeleteLogo(
    [FromServices] AppDb appDb)
  {
    var settings = await appDb.BrandingSettings.FirstOrDefaultAsync();

    if (settings is null)
    {
      return NotFound();
    }

    if (!string.IsNullOrEmpty(settings.LogoStoragePath) && Directory.Exists(settings.LogoStoragePath))
    {
      try
      {
        Directory.Delete(settings.LogoStoragePath, recursive: true);
      }
      catch
      {
        // Log but don't fail
      }
    }

    settings.LogoFileName = null;
    settings.LogoStoragePath = null;

    await appDb.SaveChangesAsync();
    return Ok(settings.ToDto());
  }
}
