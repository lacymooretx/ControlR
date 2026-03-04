using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class SavedScript : TenantEntityBase
{
  public Guid CreatorUserId { get; set; }

  [StringLength(500)]
  public string Description { get; set; } = string.Empty;

  public bool IsPublishedToClients { get; set; }

  [StringLength(200)]
  public required string Name { get; set; }

  public required string ScriptContent { get; set; }

  [StringLength(20)]
  public required string ScriptType { get; set; }
}
