using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class ClientDeviceAssignment : TenantEntityBase
{
  public AppUser? ClientUser { get; set; }

  public Guid ClientUserId { get; set; }

  public Device? Device { get; set; }

  public Guid DeviceId { get; set; }

  public DateTimeOffset? ExpiresAt { get; set; }
}
