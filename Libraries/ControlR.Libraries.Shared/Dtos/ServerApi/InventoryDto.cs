namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record SoftwareInventoryItemDto(
  Guid Id,
  Guid DeviceId,
  string Name,
  string Version,
  string Publisher,
  DateTimeOffset? InstallDate,
  DateTimeOffset LastReportedAt) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record InstalledUpdateDto(
  Guid Id,
  Guid DeviceId,
  string UpdateId,
  string Title,
  DateTimeOffset? InstalledOn,
  DateTimeOffset LastReportedAt) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record DeviceHardwareDto(
  string SerialNumber,
  string Manufacturer,
  string Model,
  string BiosVersion);

[MessagePackObject(keyAsPropertyName: true)]
public record InventorySearchRequestDto(
  string? SoftwareName,
  string? SoftwareVersion,
  string? UpdateId);

[MessagePackObject(keyAsPropertyName: true)]
public record InventorySearchResultDto(
  Guid DeviceId,
  string DeviceName,
  string SoftwareName,
  string SoftwareVersion,
  string Publisher);
