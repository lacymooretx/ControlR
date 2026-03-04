namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record DeviceGroupDto(
  Guid Id,
  string Name,
  string GroupType,
  string Description,
  Guid? ParentGroupId,
  int SortOrder,
  IReadOnlyList<Guid> DeviceIds,
  IReadOnlyList<Guid> SubGroupIds) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record DeviceGroupCreateRequestDto(
  string Name,
  string GroupType,
  string Description,
  Guid? ParentGroupId,
  int SortOrder);

[MessagePackObject(keyAsPropertyName: true)]
public record DeviceGroupUpdateRequestDto(
  Guid Id,
  string Name,
  string GroupType,
  string Description,
  Guid? ParentGroupId,
  int SortOrder);

[MessagePackObject(keyAsPropertyName: true)]
public record BulkAssignDeviceGroupRequestDto(
  Guid GroupId,
  IReadOnlyList<Guid> DeviceIds);
