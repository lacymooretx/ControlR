using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class WebhookDeliveryLog : TenantEntityBase
{
  public int AttemptNumber { get; set; }

  public DateTimeOffset AttemptedAt { get; set; }

  [StringLength(2000)]
  public string ErrorMessage { get; set; } = string.Empty;

  [StringLength(100)]
  public required string EventType { get; set; }

  public int? HttpStatusCode { get; set; }

  public bool IsSuccess { get; set; }

  [StringLength(2000)]
  public string ResponseBody { get; set; } = string.Empty;

  public WebhookSubscription? WebhookSubscription { get; set; }

  public Guid WebhookSubscriptionId { get; set; }
}
