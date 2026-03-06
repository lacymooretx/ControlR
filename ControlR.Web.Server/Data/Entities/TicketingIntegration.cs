using System.ComponentModel.DataAnnotations;
using ControlR.Libraries.Shared.Enums;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class TicketingIntegration : TenantEntityBase
{
  [StringLength(200)]
  public required string Name { get; set; }

  public TicketingProvider Provider { get; set; }

  [StringLength(500)]
  public required string BaseUrl { get; set; }

  /// <summary>
  /// Encrypted API key (ASP.NET Data Protection, base64-encoded ciphertext).
  /// </summary>
  public required string EncryptedApiKey { get; set; }

  [StringLength(200)]
  public string? DefaultProject { get; set; }

  public bool IsEnabled { get; set; } = true;

  /// <summary>
  /// Optional JSON for custom field mappings.
  /// </summary>
  public string? FieldMappingJson { get; set; }
}
