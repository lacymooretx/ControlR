namespace ControlR.Libraries.Shared.Dtos.ServerApi;

public record SessionRecordingDto(
  Guid Id,
  Guid SessionId,
  Guid? DeviceId,
  string? DeviceName,
  Guid RecorderUserId,
  string? RecorderUserName,
  DateTimeOffset SessionStartedAt,
  DateTimeOffset? SessionEndedAt,
  long DurationMs,
  int FrameCount,
  long StorageSizeBytes,
  string? Notes,
  SessionRecordingStatusDto Status,
  DateTimeOffset CreatedAt);

public enum SessionRecordingStatusDto
{
  Recording,
  Completed,
  Failed,
  Deleted
}

public record StartRecordingRequestDto(
  Guid SessionId,
  Guid DeviceId,
  string? DeviceName);

public record StopRecordingRequestDto(
  Guid RecordingId);

public record UploadRecordingFrameDto(
  Guid RecordingId,
  long TimestampMs,
  int Width,
  int Height);
