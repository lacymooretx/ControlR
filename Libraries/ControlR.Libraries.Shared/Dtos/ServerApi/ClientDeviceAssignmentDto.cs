namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record ClientDeviceAssignmentDto(
  Guid Id,
  Guid ClientUserId,
  string? ClientUserName,
  Guid DeviceId,
  string? DeviceName,
  DateTimeOffset? ExpiresAt) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record ClientDeviceAssignmentCreateRequestDto(
  Guid ClientUserId,
  Guid DeviceId,
  DateTimeOffset? ExpiresAt);

[MessagePackObject(keyAsPropertyName: true)]
public record ClientDeviceAssignmentDeleteRequestDto(
  Guid ClientUserId,
  Guid DeviceId);
