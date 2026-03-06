using System.ComponentModel.DataAnnotations;
using ControlR.Web.Server.Data.Entities.Bases;

namespace ControlR.Web.Server.Data.Entities;

public class SessionRecording : TenantEntityBase
{
  public Guid? DeviceId { get; set; }

  [StringLength(100)]
  public string? DeviceName { get; set; }

  public long DurationMs { get; set; }

  public int FrameCount { get; set; }

  [StringLength(500)]
  public string? Notes { get; set; }

  public Guid RecorderUserId { get; set; }

  [StringLength(256)]
  public string? RecorderUserName { get; set; }

  public DateTimeOffset? SessionEndedAt { get; set; }

  public Guid SessionId { get; set; }

  public DateTimeOffset SessionStartedAt { get; set; }

  public long StorageSizeBytes { get; set; }

  [StringLength(500)]
  public required string StoragePath { get; set; }

  public SessionRecordingStatus Status { get; set; } = SessionRecordingStatus.Recording;
}

public enum SessionRecordingStatus
{
  Recording,
  Completed,
  Failed,
  Deleted
}
