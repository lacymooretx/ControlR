using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/alert-rules")]
[ApiController]
[Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
public class AlertRulesController : ControllerBase
{
  [HttpPost]
  public async Task<ActionResult<AlertRuleDto>> CreateAlertRule(
    [FromServices] AppDb appDb,
    [FromBody] AlertRuleCreateRequestDto dto)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    var rule = new AlertRule
    {
      CreatorUserId = userId,
      Duration = dto.Duration,
      MetricType = dto.MetricType,
      Name = dto.Name,
      NotificationRecipients = dto.NotificationRecipients,
      Operator = dto.Operator,
      TargetDeviceIds = dto.TargetDeviceIds.ToList(),
      TargetGroupIds = dto.TargetGroupIds.ToList(),
      TenantId = tenantId,
      ThresholdValue = dto.ThresholdValue,
    };

    await appDb.AlertRules.AddAsync(rule);
    await appDb.SaveChangesAsync();

    return Ok(rule.ToDto());
  }

  [HttpDelete("{ruleId:guid}")]
  public async Task<ActionResult> DeleteAlertRule(
    [FromServices] AppDb appDb,
    [FromRoute] Guid ruleId)
  {
    var rule = await appDb.AlertRules
      .FirstOrDefaultAsync(x => x.Id == ruleId);

    if (rule is null)
    {
      return NotFound();
    }

    appDb.AlertRules.Remove(rule);
    await appDb.SaveChangesAsync();

    return NoContent();
  }

  [HttpGet]
  public async Task<ActionResult<AlertRuleDto[]>> GetAllAlertRules(
    [FromServices] AppDb appDb)
  {
    var rules = await appDb.AlertRules
      .AsNoTracking()
      .OrderBy(x => x.Name)
      .ToListAsync();

    var dtos = rules.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpPut]
  public async Task<ActionResult<AlertRuleDto>> UpdateAlertRule(
    [FromServices] AppDb appDb,
    [FromBody] AlertRuleUpdateRequestDto dto)
  {
    var rule = await appDb.AlertRules
      .FirstOrDefaultAsync(x => x.Id == dto.Id);

    if (rule is null)
    {
      return NotFound();
    }

    rule.Duration = dto.Duration;
    rule.IsEnabled = dto.IsEnabled;
    rule.MetricType = dto.MetricType;
    rule.Name = dto.Name;
    rule.NotificationRecipients = dto.NotificationRecipients;
    rule.Operator = dto.Operator;
    rule.TargetDeviceIds = dto.TargetDeviceIds.ToList();
    rule.TargetGroupIds = dto.TargetGroupIds.ToList();
    rule.ThresholdValue = dto.ThresholdValue;

    await appDb.SaveChangesAsync();

    return Ok(rule.ToDto());
  }
}
