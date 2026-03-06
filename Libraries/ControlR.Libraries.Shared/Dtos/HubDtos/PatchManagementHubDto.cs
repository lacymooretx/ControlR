namespace ControlR.Libraries.Shared.Dtos.HubDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record PatchScanRequestHubDto();

[MessagePackObject(keyAsPropertyName: true)]
public record PatchScanResultHubDto(
  Guid DeviceId,
  PatchInfoHubDto[] AvailablePatches);

[MessagePackObject(keyAsPropertyName: true)]
public record PatchInfoHubDto(
  string UpdateId,
  string Title,
  string? Description,
  bool IsImportant,
  bool IsCritical,
  long SizeBytes);

[MessagePackObject(keyAsPropertyName: true)]
public record PatchInstallRequestHubDto(string[] UpdateIds);

[MessagePackObject(keyAsPropertyName: true)]
public record PatchInstallResultHubDto(
  Guid DeviceId,
  bool IsSuccess,
  string? ErrorMessage,
  int InstalledCount,
  int FailedCount);
