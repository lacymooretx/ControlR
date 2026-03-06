using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class PluginRegistration : TenantEntityBase
{
  [StringLength(200)]
  public required string Name { get; set; }

  [StringLength(500)]
  public required string AssemblyPath { get; set; }

  [StringLength(500)]
  public required string PluginTypeName { get; set; }

  public bool IsEnabled { get; set; } = true;

  public string? ConfigurationJson { get; set; }

  public DateTimeOffset? LastLoadedAt { get; set; }
}
