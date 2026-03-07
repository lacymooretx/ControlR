using ControlR.Libraries.Shared.Dtos.ServerApi;

namespace ControlR.Web.Client.Services;

public interface ITenantSwitcherService
{
  Guid? SelectedTenantId { get; }
  string? SelectedTenantName { get; }
  event Action? OnTenantChanged;
  void SwitchTenant(Guid? tenantId, string? tenantName);
  void ClearOverride();
}

public class TenantSwitcherService : ITenantSwitcherService
{
  public Guid? SelectedTenantId { get; private set; }
  public string? SelectedTenantName { get; private set; }
  public event Action? OnTenantChanged;

  public void SwitchTenant(Guid? tenantId, string? tenantName)
  {
    SelectedTenantId = tenantId;
    SelectedTenantName = tenantName;
    OnTenantChanged?.Invoke();
  }

  public void ClearOverride()
  {
    SelectedTenantId = null;
    SelectedTenantName = null;
    OnTenantChanged?.Invoke();
  }
}
