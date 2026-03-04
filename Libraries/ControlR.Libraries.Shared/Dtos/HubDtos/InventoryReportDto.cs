namespace ControlR.Libraries.Shared.Dtos.HubDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record InventoryReportHubDto(
  Guid DeviceId,
  List<SoftwareItemHubDto> Software,
  List<InstalledUpdateHubDto> Updates,
  HardwareInfoHubDto Hardware);

[MessagePackObject(keyAsPropertyName: true)]
public record SoftwareItemHubDto(
  string Name,
  string Version,
  string Publisher,
  DateTimeOffset? InstallDate);

[MessagePackObject(keyAsPropertyName: true)]
public record InstalledUpdateHubDto(
  string UpdateId,
  string Title,
  DateTimeOffset? InstalledOn);

[MessagePackObject(keyAsPropertyName: true)]
public record HardwareInfoHubDto(
  string SerialNumber,
  string Manufacturer,
  string Model,
  string BiosVersion);
