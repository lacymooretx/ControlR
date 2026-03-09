using System.Runtime.Loader;
using ControlR.Libraries.Shared.Plugins;

namespace ControlR.Web.Server.Services;

public interface IPluginLoaderService
{
  Task LoadPlugins();
  IReadOnlyList<LoadedPlugin> GetLoadedPlugins();
  Task NotifyDeviceHeartbeat(Guid deviceId, Guid tenantId);
  Task NotifySessionStart(Guid sessionId, Guid deviceId, Guid userId);
  Task NotifySessionEnd(Guid sessionId, Guid deviceId, Guid userId);
}

public record LoadedPlugin(
  Guid RegistrationId,
  IControlRPlugin Instance,
  Guid TenantId);

public class PluginLoaderService(
  IServiceProvider serviceProvider,
  IDbContextFactory<AppDb> dbContextFactory,
  ILogger<PluginLoaderService> logger) : IPluginLoaderService
{
  private readonly List<LoadedPlugin> _loadedPlugins = [];
  private readonly Lock _lock = new();

  public IReadOnlyList<LoadedPlugin> GetLoadedPlugins()
  {
    lock (_lock)
    {
      return _loadedPlugins.ToList().AsReadOnly();
    }
  }

  public async Task LoadPlugins()
  {
    await using var db = await dbContextFactory.CreateDbContextAsync();

    var registrations = await db.PluginRegistrations
      .Where(p => p.IsEnabled)
      .ToListAsync();

    lock (_lock)
    {
      _loadedPlugins.Clear();
    }

    foreach (var registration in registrations)
    {
      try
      {
        var plugin = LoadPlugin(registration);
        if (plugin is null)
        {
          continue;
        }

        await plugin.Initialize(serviceProvider);

        lock (_lock)
        {
          _loadedPlugins.Add(new LoadedPlugin(
            registration.Id,
            plugin,
            registration.TenantId));
        }

        registration.LastLoadedAt = DateTimeOffset.UtcNow;

        logger.LogInformation(
          "Loaded plugin '{Name}' v{Version} for tenant {TenantId}.",
          plugin.Name,
          plugin.Version,
          registration.TenantId);
      }
      catch (Exception ex)
      {
        logger.LogError(ex,
          "Failed to load plugin '{Name}' from '{AssemblyPath}'.",
          registration.Name,
          registration.AssemblyPath);
      }
    }

    await db.SaveChangesAsync();
  }

  public async Task NotifyDeviceHeartbeat(Guid deviceId, Guid tenantId)
  {
    var plugins = GetPluginsForTenant(tenantId);

    foreach (var plugin in plugins)
    {
      try
      {
        await plugin.Instance.OnDeviceHeartbeat(deviceId, tenantId);
      }
      catch (Exception ex)
      {
        logger.LogError(ex,
          "Plugin '{Name}' threw during OnDeviceHeartbeat for device {DeviceId}.",
          plugin.Instance.Name,
          deviceId);
      }
    }
  }

  public async Task NotifySessionStart(Guid sessionId, Guid deviceId, Guid userId)
  {
    var plugins = GetLoadedPlugins();

    foreach (var plugin in plugins)
    {
      try
      {
        await plugin.Instance.OnSessionStart(sessionId, deviceId, userId);
      }
      catch (Exception ex)
      {
        logger.LogError(ex,
          "Plugin '{Name}' threw during OnSessionStart for session {SessionId}.",
          plugin.Instance.Name,
          sessionId);
      }
    }
  }

  public async Task NotifySessionEnd(Guid sessionId, Guid deviceId, Guid userId)
  {
    var plugins = GetLoadedPlugins();

    foreach (var plugin in plugins)
    {
      try
      {
        await plugin.Instance.OnSessionEnd(sessionId, deviceId, userId);
      }
      catch (Exception ex)
      {
        logger.LogError(ex,
          "Plugin '{Name}' threw during OnSessionEnd for session {SessionId}.",
          plugin.Instance.Name,
          sessionId);
      }
    }
  }

  private IReadOnlyList<LoadedPlugin> GetPluginsForTenant(Guid tenantId)
  {
    lock (_lock)
    {
      return _loadedPlugins
        .Where(p => p.TenantId == tenantId)
        .ToList()
        .AsReadOnly();
    }
  }

  private IControlRPlugin? LoadPlugin(PluginRegistration registration)
  {
    var pluginsBasePath = Path.GetFullPath(Path.Combine(
      AppContext.BaseDirectory,
      "plugins"));

    if (Path.IsPathRooted(registration.AssemblyPath))
    {
      logger.LogWarning(
        "Plugin assembly path must be relative to plugins directory. Skipping '{AssemblyPath}' for plugin '{Name}'.",
        registration.AssemblyPath,
        registration.Name);
      return null;
    }

    var assemblyPath = Path.GetFullPath(
      registration.AssemblyPath,
      pluginsBasePath);

    if (!IsSubPathOf(assemblyPath, pluginsBasePath))
    {
      logger.LogWarning(
        "Plugin assembly path '{AssemblyPath}' is outside plugins directory. Skipping plugin '{Name}'.",
        assemblyPath,
        registration.Name);
      return null;
    }

    if (!string.Equals(Path.GetExtension(assemblyPath), ".dll", StringComparison.OrdinalIgnoreCase))
    {
      logger.LogWarning(
        "Plugin assembly path '{AssemblyPath}' is not a .dll. Skipping plugin '{Name}'.",
        assemblyPath,
        registration.Name);
      return null;
    }

    if (!File.Exists(assemblyPath))
    {
      logger.LogWarning(
        "Plugin assembly not found at '{AssemblyPath}' for plugin '{Name}'.",
        assemblyPath,
        registration.Name);
      return null;
    }

    var loadContext = new AssemblyLoadContext(
      $"Plugin_{registration.Id}",
      isCollectible: true);

    var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
    var pluginType = assembly.GetType(registration.PluginTypeName);

    if (pluginType is null)
    {
      logger.LogWarning(
        "Plugin type '{TypeName}' not found in assembly '{AssemblyPath}'.",
        registration.PluginTypeName,
        assemblyPath);
      loadContext.Unload();
      return null;
    }

    if (!typeof(IControlRPlugin).IsAssignableFrom(pluginType))
    {
      logger.LogWarning(
        "Type '{TypeName}' does not implement IControlRPlugin.",
        registration.PluginTypeName);
      loadContext.Unload();
      return null;
    }

    var instance = Activator.CreateInstance(pluginType) as IControlRPlugin;
    if (instance is null)
    {
      logger.LogWarning(
        "Failed to create instance of plugin type '{TypeName}'.",
        registration.PluginTypeName);
      loadContext.Unload();
      return null;
    }

    return instance;
  }

  private static bool IsSubPathOf(string path, string basePath)
  {
    var comparison = OperatingSystem.IsWindows()
      ? StringComparison.OrdinalIgnoreCase
      : StringComparison.Ordinal;

    var normalizedBasePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
      + Path.DirectorySeparatorChar;

    return path.StartsWith(normalizedBasePath, comparison);
  }
}
