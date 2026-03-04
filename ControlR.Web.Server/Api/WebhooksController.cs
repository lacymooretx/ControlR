using System.Security.Claims;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/webhooks")]
[ApiController]
[Authorize(Roles = RoleNames.TenantAdministrator)]
public class WebhooksController : ControllerBase
{
  [HttpPost]
  public async Task<ActionResult<WebhookSubscriptionDto>> CreateWebhook(
    [FromServices] AppDb appDb,
    [FromBody] WebhookCreateRequestDto request)
  {
    var tenantId = User.FindFirstValue("TenantId");
    if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
    {
      return BadRequest("Invalid tenant.");
    }

    var subscription = new WebhookSubscription
    {
      EventTypes = request.EventTypes,
      Name = request.Name,
      Secret = request.Secret,
      TenantId = tenantGuid,
      Url = request.Url,
    };

    await appDb.WebhookSubscriptions.AddAsync(subscription);
    await appDb.SaveChangesAsync();

    return Ok(subscription.ToDto());
  }

  [HttpDelete("{webhookId:guid}")]
  public async Task<ActionResult> DeleteWebhook(
    [FromServices] AppDb appDb,
    [FromRoute] Guid webhookId)
  {
    var subscription = await appDb.WebhookSubscriptions.FindAsync(webhookId);
    if (subscription is null)
    {
      return NotFound();
    }

    appDb.WebhookSubscriptions.Remove(subscription);
    await appDb.SaveChangesAsync();

    return NoContent();
  }

  [HttpGet]
  public async Task<ActionResult<WebhookSubscriptionDto[]>> GetAllWebhooks(
    [FromServices] AppDb appDb)
  {
    var subscriptions = await appDb.WebhookSubscriptions
      .AsNoTracking()
      .OrderBy(s => s.Name)
      .ToListAsync();

    var dtos = subscriptions.Select(s => s.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("{webhookId:guid}/deliveries")]
  public async Task<ActionResult<WebhookDeliveryLogDto[]>> GetDeliveries(
    [FromServices] AppDb appDb,
    [FromRoute] Guid webhookId,
    [FromQuery] int count = 50)
  {
    var deliveries = await appDb.WebhookDeliveryLogs
      .AsNoTracking()
      .Where(d => d.WebhookSubscriptionId == webhookId)
      .OrderByDescending(d => d.AttemptedAt)
      .Take(count)
      .ToListAsync();

    var dtos = deliveries.Select(d => d.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpPost("{webhookId:guid}/test")]
  public async Task<ActionResult> TestWebhook(
    [FromServices] AppDb appDb,
    [FromServices] IWebhookDispatcher webhookDispatcher,
    [FromRoute] Guid webhookId)
  {
    var subscription = await appDb.WebhookSubscriptions.FindAsync(webhookId);
    if (subscription is null)
    {
      return NotFound();
    }

    webhookDispatcher.Dispatch("webhook.test", subscription.TenantId, new
    {
      message = "This is a test webhook from ControlR.",
      webhookId = subscription.Id,
      webhookName = subscription.Name,
      timestamp = DateTimeOffset.UtcNow,
    });

    return Ok();
  }

  [HttpPut]
  public async Task<ActionResult<WebhookSubscriptionDto>> UpdateWebhook(
    [FromServices] AppDb appDb,
    [FromBody] WebhookUpdateRequestDto request)
  {
    var subscription = await appDb.WebhookSubscriptions.FindAsync(request.Id);
    if (subscription is null)
    {
      return NotFound();
    }

    subscription.EventTypes = request.EventTypes;
    subscription.IsEnabled = request.IsEnabled;
    subscription.Name = request.Name;
    subscription.Url = request.Url;

    if (!string.IsNullOrEmpty(request.Secret))
    {
      subscription.Secret = request.Secret;
    }

    // Re-enable if previously disabled
    if (request.IsEnabled && subscription.IsDisabledDueToFailures)
    {
      subscription.FailureCount = 0;
      subscription.IsDisabledDueToFailures = false;
    }

    await appDb.SaveChangesAsync();

    return Ok(subscription.ToDto());
  }
}
