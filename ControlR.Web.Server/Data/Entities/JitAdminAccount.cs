using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class JitAdminAccount : TenantEntityBase
{
  public Guid DeviceId { get; set; }

  [StringLength(100)]
  public string? DeviceName { get; set; }

  [StringLength(64)]
  public required string Username { get; set; }

  public Guid CreatedByUserId { get; set; }

  [StringLength(256)]
  public string? CreatedByUserName { get; set; }

  public DateTimeOffset ExpiresAt { get; set; }

  public DateTimeOffset? DeletedAt { get; set; }

  public JitAdminAccountStatus Status { get; set; } = JitAdminAccountStatus.Active;
}

public enum JitAdminAccountStatus
{
  Active,
  Expired,
  ManuallyDeleted,
  Failed
}
