namespace ControlR.Web.Client.StateManagement.Stores;

public interface IDeviceGroupStore : IStoreBase<DeviceGroupDto>
{ }

internal class DeviceGroupStore(
  IControlrApi controlrApi,
  ISnackbar snackbar,
  ILogger<DeviceGroupStore> logger)
  : StoreBase<DeviceGroupDto>(controlrApi, snackbar, logger), IDeviceGroupStore
{
  protected override async Task RefreshImpl()
  {
    var result = await ControlrApi.GetAllDeviceGroups();
    if (!result.IsSuccess)
    {
      Snackbar.Add("Failed to load device groups", Severity.Error);
      return;
    }

    Cache.Clear();
    foreach (var group in result.Value ?? [])
    {
      Cache.AddOrUpdate(group.Id, group, (_, _) => group);
    }
  }
}
