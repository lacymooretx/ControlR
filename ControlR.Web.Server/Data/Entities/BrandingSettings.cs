using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class BrandingSettings : TenantEntityBase
{
  [StringLength(100)]
  public string ProductName { get; set; } = "ControlR";

  [StringLength(20)]
  public string PrimaryColor { get; set; } = "#2196F3";

  [StringLength(20)]
  public string? SecondaryColor { get; set; }

  [StringLength(260)]
  public string? LogoFileName { get; set; }

  [StringLength(500)]
  public string? LogoStoragePath { get; set; }

  [StringLength(260)]
  public string? FaviconFileName { get; set; }
}
