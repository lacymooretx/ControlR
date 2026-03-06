namespace ControlR.Libraries.Shared.Plugins;

public interface IControlRPlugin
{
  string Name { get; }
  string Version { get; }
  string? Description { get; }
  Task Initialize(IServiceProvider services);
  Task OnDeviceHeartbeat(Guid deviceId, Guid tenantId);
  Task OnSessionStart(Guid sessionId, Guid deviceId, Guid userId);
  Task OnSessionEnd(Guid sessionId, Guid deviceId, Guid userId);
}
