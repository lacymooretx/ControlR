using System.ComponentModel.DataAnnotations;
using ControlR.Libraries.Shared.Enums;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class TicketLink : TenantEntityBase
{
  [StringLength(500)]
  public required string ExternalTicketId { get; set; }

  [StringLength(2000)]
  public required string ExternalTicketUrl { get; set; }

  public TicketingProvider Provider { get; set; }

  [StringLength(500)]
  public required string Subject { get; set; }

  public Guid? DeviceId { get; set; }

  public Guid? SessionId { get; set; }

  public Guid? AlertId { get; set; }

  public Guid CreatedByUserId { get; set; }
}
