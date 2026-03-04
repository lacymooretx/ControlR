using System.Globalization;
using System.Text;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/audit-logs")]
[ApiController]
[Authorize]
public class AuditLogController : ControllerBase
{
  [HttpGet("export")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<IActionResult> ExportAuditLogs(
    [FromServices] AppDb appDb,
    [FromQuery] DateTimeOffset? startDate,
    [FromQuery] DateTimeOffset? endDate)
  {
    var query = appDb.AuditLogs.AsNoTracking().AsQueryable();

    if (startDate.HasValue)
    {
      query = query.Where(x => x.Timestamp >= startDate.Value);
    }

    if (endDate.HasValue)
    {
      query = query.Where(x => x.Timestamp <= endDate.Value);
    }

    var entries = await query
      .OrderByDescending(x => x.Timestamp)
      .ToListAsync();

    var csv = new StringBuilder();
    csv.AppendLine("Timestamp,EventType,Action,ActorUserName,TargetDeviceName,SourceIpAddress,Details,SessionId");

    foreach (var entry in entries)
    {
      csv.AppendLine(string.Join(",",
        EscapeCsvField(entry.Timestamp.ToString("o", CultureInfo.InvariantCulture)),
        EscapeCsvField(entry.EventType),
        EscapeCsvField(entry.Action),
        EscapeCsvField(entry.ActorUserName ?? ""),
        EscapeCsvField(entry.TargetDeviceName ?? ""),
        EscapeCsvField(entry.SourceIpAddress ?? ""),
        EscapeCsvField(entry.Details ?? ""),
        EscapeCsvField(entry.SessionId?.ToString() ?? "")));
    }

    return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "audit-logs.csv");
  }

  [HttpGet("{id:guid}")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<AuditLogDto>> GetAuditLog(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var entry = await appDb.AuditLogs
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == id);

    if (entry is null)
    {
      return NotFound();
    }

    return Ok(new AuditLogDto(
      entry.Id,
      entry.EventType,
      entry.Action,
      entry.ActorUserId,
      entry.ActorUserName,
      entry.TargetDeviceId,
      entry.TargetDeviceName,
      entry.SourceIpAddress,
      entry.Timestamp,
      entry.EndTimestamp,
      entry.Duration,
      entry.Details,
      entry.SessionId));
  }

  [HttpPost("search")]
  [Authorize(Roles = RoleNames.TenantAdministrator)]
  public async Task<ActionResult<AuditLogSearchResponseDto>> SearchAuditLogs(
    [FromServices] AppDb appDb,
    [FromBody] AuditLogSearchRequestDto request)
  {
    var query = appDb.AuditLogs.AsNoTracking().AsQueryable();

    if (request.StartDate.HasValue)
    {
      query = query.Where(x => x.Timestamp >= request.StartDate.Value);
    }

    if (request.EndDate.HasValue)
    {
      query = query.Where(x => x.Timestamp <= request.EndDate.Value);
    }

    if (!string.IsNullOrWhiteSpace(request.EventType))
    {
      query = query.Where(x => x.EventType == request.EventType);
    }

    if (!string.IsNullOrWhiteSpace(request.ActorUserName))
    {
      query = query.Where(x => x.ActorUserName != null && x.ActorUserName.Contains(request.ActorUserName));
    }

    if (request.TargetDeviceId.HasValue)
    {
      query = query.Where(x => x.TargetDeviceId == request.TargetDeviceId.Value);
    }

    if (!string.IsNullOrWhiteSpace(request.SearchText))
    {
      query = query.Where(x =>
        (x.Details != null && x.Details.Contains(request.SearchText)) ||
        (x.ActorUserName != null && x.ActorUserName.Contains(request.SearchText)) ||
        (x.TargetDeviceName != null && x.TargetDeviceName.Contains(request.SearchText)));
    }

    var totalCount = await query.CountAsync();

    var items = await query
      .OrderByDescending(x => x.Timestamp)
      .Skip(request.Skip)
      .Take(request.Take)
      .Select(x => new AuditLogDto(
        x.Id,
        x.EventType,
        x.Action,
        x.ActorUserId,
        x.ActorUserName,
        x.TargetDeviceId,
        x.TargetDeviceName,
        x.SourceIpAddress,
        x.Timestamp,
        x.EndTimestamp,
        x.Duration,
        x.Details,
        x.SessionId))
      .ToListAsync();

    return Ok(new AuditLogSearchResponseDto(items, totalCount));
  }

  private static string EscapeCsvField(string field)
  {
    if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
    {
      return $"\"{field.Replace("\"", "\"\"")}\"";
    }
    return field;
  }
}
