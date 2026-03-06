using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class SupportSession : TenantEntityBase
{
  [StringLength(20)]
  public required string AccessCode { get; set; }

  [StringLength(200)]
  public string? ClientName { get; set; }

  [StringLength(500)]
  public string? ClientEmail { get; set; }

  public Guid CreatorUserId { get; set; }

  [StringLength(200)]
  public string? CreatorUserName { get; set; }

  public Guid? DeviceId { get; set; }

  [StringLength(100)]
  public string? DeviceName { get; set; }

  public DateTimeOffset ExpiresAt { get; set; }

  public bool IsUsed { get; set; }

  [StringLength(200)]
  public string? Notes { get; set; }

  public DateTimeOffset? SessionEndedAt { get; set; }

  public DateTimeOffset? SessionStartedAt { get; set; }

  public SupportSessionStatus Status { get; set; } = SupportSessionStatus.Pending;
}

public enum SupportSessionStatus
{
  Pending,
  WaitingForClient,
  InProgress,
  Completed,
  Expired,
  Cancelled
}
