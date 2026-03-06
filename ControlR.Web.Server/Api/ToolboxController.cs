using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/toolbox")]
[ApiController]
[Authorize(Roles = RoleNames.TenantAdministrator)]
public class ToolboxController : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<ToolboxItemDto[]>> GetItems(
    [FromServices] AppDb appDb)
  {
    var items = await appDb.ToolboxItems
      .AsNoTracking()
      .OrderByDescending(x => x.CreatedAt)
      .ToListAsync();

    var dtos = items.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("{id:guid}")]
  public async Task<ActionResult<ToolboxItemDto>> GetItem(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var item = await appDb.ToolboxItems
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == id);

    if (item is null)
    {
      return NotFound();
    }

    return Ok(item.ToDto());
  }

  [HttpPost]
  [RequestSizeLimit(500 * 1024 * 1024)]
  public async Task<ActionResult<ToolboxItemDto>> UploadItem(
    [FromServices] AppDb appDb,
    [FromServices] IOptions<AppOptions> appOptions)
  {
    var tenantId = User.FindFirstValue("TenantId");
    if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
    {
      return BadRequest("Invalid tenant.");
    }

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
    {
      return BadRequest("Invalid user.");
    }

    var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);

    if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
    {
      return BadRequest("No file uploaded.");
    }

    var file = Request.Form.Files[0];

    ToolboxItemCreateRequestDto? metadata = null;
    if (Request.Form.TryGetValue("metadata", out var metadataJson))
    {
      metadata = JsonSerializer.Deserialize<ToolboxItemCreateRequestDto>(
        metadataJson.ToString(),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    var itemId = Guid.NewGuid();
    var storagePath = Path.Combine(
      appOptions.Value.ToolboxStoragePath,
      tenantGuid.ToString(),
      itemId.ToString());

    Directory.CreateDirectory(storagePath);

    var filePath = Path.Combine(storagePath, file.FileName);
    string sha256Hash;

    await using (var stream = new FileStream(filePath, FileMode.Create))
    {
      await file.CopyToAsync(stream);
    }

    await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
    {
      var hashBytes = await SHA256.HashDataAsync(stream);
      sha256Hash = Convert.ToHexStringLower(hashBytes);
    }

    var item = new ToolboxItem
    {
      Id = itemId,
      Name = metadata?.Name ?? Path.GetFileNameWithoutExtension(file.FileName),
      Description = metadata?.Description,
      FileName = file.FileName,
      Category = metadata?.Category,
      Version = metadata?.Version,
      FileSizeBytes = file.Length,
      StoragePath = storagePath,
      Sha256Hash = sha256Hash,
      UploadedByUserId = userGuid,
      UploadedByUserName = userName,
      TenantId = tenantGuid,
    };

    await appDb.ToolboxItems.AddAsync(item);
    await appDb.SaveChangesAsync();

    return Ok(item.ToDto());
  }

  [HttpPut("{id:guid}")]
  public async Task<ActionResult<ToolboxItemDto>> UpdateItem(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id,
    [FromBody] ToolboxItemUpdateRequestDto request)
  {
    var item = await appDb.ToolboxItems.FindAsync(id);
    if (item is null)
    {
      return NotFound();
    }

    item.Name = request.Name;
    item.Description = request.Description;
    item.Category = request.Category;
    item.Version = request.Version;

    await appDb.SaveChangesAsync();

    return Ok(item.ToDto());
  }

  [HttpDelete("{id:guid}")]
  public async Task<ActionResult> DeleteItem(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var item = await appDb.ToolboxItems.FindAsync(id);
    if (item is null)
    {
      return NotFound();
    }

    if (Directory.Exists(item.StoragePath))
    {
      try
      {
        Directory.Delete(item.StoragePath, recursive: true);
      }
      catch
      {
        // Log but don't fail the request
      }
    }

    appDb.ToolboxItems.Remove(item);
    await appDb.SaveChangesAsync();

    return NoContent();
  }

  [HttpGet("{id:guid}/download")]
  public async Task<ActionResult> DownloadItem(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var item = await appDb.ToolboxItems
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == id);

    if (item is null)
    {
      return NotFound();
    }

    var filePath = Path.Combine(item.StoragePath, item.FileName);
    if (!System.IO.File.Exists(filePath))
    {
      return NotFound("File not found on disk.");
    }

    var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    return File(stream, "application/octet-stream", item.FileName);
  }
}
