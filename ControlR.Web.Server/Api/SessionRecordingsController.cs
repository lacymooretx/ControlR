using System.Security.Claims;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/session-recordings")]
[ApiController]
[Authorize]
public class SessionRecordingsController : ControllerBase
{
  [HttpPost("start")]
  public async Task<ActionResult<SessionRecordingDto>> StartRecording(
    [FromServices] AppDb appDb,
    [FromServices] IOptions<AppOptions> appOptions,
    [FromBody] StartRecordingRequestDto request)
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

    var storagePath = Path.Combine(
      appOptions.Value.RecordingsStoragePath,
      tenantGuid.ToString(),
      Guid.NewGuid().ToString());

    Directory.CreateDirectory(storagePath);

    var recording = new SessionRecording
    {
      DeviceId = request.DeviceId,
      DeviceName = request.DeviceName,
      RecorderUserId = userGuid,
      RecorderUserName = userName,
      SessionId = request.SessionId,
      SessionStartedAt = DateTimeOffset.UtcNow,
      StoragePath = storagePath,
      Status = SessionRecordingStatus.Recording,
      TenantId = tenantGuid,
    };

    await appDb.SessionRecordings.AddAsync(recording);
    await appDb.SaveChangesAsync();

    return Ok(recording.ToDto());
  }

  [HttpPost("{id:guid}/frame")]
  [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB per frame upload
  public async Task<ActionResult> UploadFrame(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var tenantId = User.FindFirstValue("TenantId");
    if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
    {
      return BadRequest("Invalid tenant.");
    }

    var recording = await appDb.SessionRecordings.FindAsync(id);
    if (recording is null)
    {
      return NotFound();
    }

    if (recording.TenantId != tenantGuid)
    {
      return Forbid();
    }

    if (recording.Status != SessionRecordingStatus.Recording)
    {
      return BadRequest("Recording is not in progress.");
    }

    if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
    {
      return BadRequest("No file uploaded.");
    }

    var file = Request.Form.Files[0];
    if (!Request.Form.TryGetValue("timestampMs", out var timestampStr) ||
        !long.TryParse(timestampStr, out var timestampMs))
    {
      return BadRequest("Missing or invalid timestampMs.");
    }

    var filePath = Path.Combine(recording.StoragePath, $"{timestampMs}.jpg");

    await using (var stream = new FileStream(filePath, FileMode.Create))
    {
      await file.CopyToAsync(stream);
    }

    recording.FrameCount++;
    recording.StorageSizeBytes += file.Length;
    await appDb.SaveChangesAsync();

    return Ok();
  }

  [HttpPost("{id:guid}/stop")]
  public async Task<ActionResult<SessionRecordingDto>> StopRecording(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var tenantId = User.FindFirstValue("TenantId");
    if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
    {
      return BadRequest("Invalid tenant.");
    }

    var recording = await appDb.SessionRecordings.FindAsync(id);
    if (recording is null)
    {
      return NotFound();
    }

    if (recording.TenantId != tenantGuid)
    {
      return Forbid();
    }

    if (recording.Status != SessionRecordingStatus.Recording)
    {
      return BadRequest("Recording is not in progress.");
    }

    recording.SessionEndedAt = DateTimeOffset.UtcNow;
    recording.DurationMs = (long)(recording.SessionEndedAt.Value - recording.SessionStartedAt).TotalMilliseconds;
    recording.Status = SessionRecordingStatus.Completed;

    // Recount frames and size from disk for accuracy
    if (Directory.Exists(recording.StoragePath))
    {
      var files = Directory.GetFiles(recording.StoragePath, "*.jpg");
      recording.FrameCount = files.Length;
      recording.StorageSizeBytes = files.Sum(f => new FileInfo(f).Length);
    }

    await appDb.SaveChangesAsync();

    return Ok(recording.ToDto());
  }

  [HttpGet]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<SessionRecordingDto[]>> GetRecordings(
    [FromServices] AppDb appDb)
  {
    var recordings = await appDb.SessionRecordings
      .AsNoTracking()
      .OrderByDescending(r => r.SessionStartedAt)
      .ToListAsync();

    var dtos = recordings.Select(r => r.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("{id:guid}")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<SessionRecordingDto>> GetRecording(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var recording = await appDb.SessionRecordings
      .AsNoTracking()
      .FirstOrDefaultAsync(r => r.Id == id);

    if (recording is null)
    {
      return NotFound();
    }

    return Ok(recording.ToDto());
  }

  [HttpGet("{id:guid}/frames")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<long[]>> GetFrameList(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var recording = await appDb.SessionRecordings
      .AsNoTracking()
      .FirstOrDefaultAsync(r => r.Id == id);

    if (recording is null)
    {
      return NotFound();
    }

    if (!Directory.Exists(recording.StoragePath))
    {
      return Ok(Array.Empty<long>());
    }

    var timestamps = Directory.GetFiles(recording.StoragePath, "*.jpg")
      .Select(f => Path.GetFileNameWithoutExtension(f))
      .Where(name => long.TryParse(name, out _))
      .Select(name => long.Parse(name))
      .OrderBy(t => t)
      .ToArray();

    return Ok(timestamps);
  }

  [HttpGet("{id:guid}/frames/{timestampMs:long}")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult> GetFrame(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id,
    [FromRoute] long timestampMs)
  {
    var recording = await appDb.SessionRecordings
      .AsNoTracking()
      .FirstOrDefaultAsync(r => r.Id == id);

    if (recording is null)
    {
      return NotFound();
    }

    var filePath = Path.Combine(recording.StoragePath, $"{timestampMs}.jpg");
    if (!System.IO.File.Exists(filePath))
    {
      return NotFound();
    }

    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
    return File(fileBytes, "image/jpeg");
  }

  [HttpDelete("{id:guid}")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult> DeleteRecording(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var recording = await appDb.SessionRecordings.FindAsync(id);
    if (recording is null)
    {
      return NotFound();
    }

    // Delete files from disk
    if (Directory.Exists(recording.StoragePath))
    {
      try
      {
        Directory.Delete(recording.StoragePath, recursive: true);
      }
      catch
      {
        // Log but don't fail the request
      }
    }

    recording.Status = SessionRecordingStatus.Deleted;
    recording.StorageSizeBytes = 0;
    recording.FrameCount = 0;
    await appDb.SaveChangesAsync();

    return NoContent();
  }
}
