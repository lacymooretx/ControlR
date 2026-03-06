using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class ToolboxItem : TenantEntityBase
{
  [StringLength(200)]
  public required string Name { get; set; }

  [StringLength(500)]
  public string? Description { get; set; }

  [StringLength(260)]
  public required string FileName { get; set; }

  [StringLength(50)]
  public string? Category { get; set; }

  [StringLength(50)]
  public string? Version { get; set; }

  public long FileSizeBytes { get; set; }

  [StringLength(500)]
  public required string StoragePath { get; set; }

  [StringLength(64)]
  public string? Sha256Hash { get; set; }

  public Guid UploadedByUserId { get; set; }

  [StringLength(256)]
  public string? UploadedByUserName { get; set; }

  public int DeploymentCount { get; set; }
}
