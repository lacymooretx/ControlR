using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class WebhookSubscription : TenantEntityBase
{
  public string[] EventTypes { get; set; } = [];

  public int FailureCount { get; set; }

  public bool IsDisabledDueToFailures { get; set; }

  public bool IsEnabled { get; set; } = true;

  public int? LastStatus { get; set; }

  public DateTimeOffset? LastTriggeredAt { get; set; }

  [StringLength(200)]
  public required string Name { get; set; }

  [StringLength(200)]
  public required string Secret { get; set; }

  [StringLength(500)]
  public required string Url { get; set; }
}
