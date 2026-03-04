using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class DeviceMetricSnapshot : TenantEntityBase
{
  public double CpuPercent { get; set; }

  public Guid DeviceId { get; set; }

  public double DiskPercent { get; set; }

  public double MemoryPercent { get; set; }

  public DateTimeOffset Timestamp { get; set; }
}
