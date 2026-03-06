using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class StoredCredential : TenantEntityBase
{
  [StringLength(200)]
  public required string Name { get; set; }

  [StringLength(500)]
  public string? Description { get; set; }

  [StringLength(256)]
  public required string Username { get; set; }

  /// <summary>
  /// Encrypted password (ASP.NET Data Protection, base64-encoded ciphertext).
  /// </summary>
  public required string EncryptedPassword { get; set; }

  [StringLength(200)]
  public string? Domain { get; set; }

  /// <summary>
  /// Scope: null = tenant-wide, non-null = specific device.
  /// </summary>
  public Guid? DeviceId { get; set; }

  /// <summary>
  /// Scope: null = tenant-wide, non-null = specific device group.
  /// </summary>
  public Guid? DeviceGroupId { get; set; }

  [StringLength(50)]
  public string? Category { get; set; }

  public Guid CreatedByUserId { get; set; }

  [StringLength(256)]
  public string? CreatedByUserName { get; set; }

  public DateTimeOffset? LastAccessedAt { get; set; }

  public int AccessCount { get; set; }
}
