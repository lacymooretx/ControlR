namespace ControlR.Libraries.Shared.Dtos.ServerApi;

public record PluginRegistrationDto(
  Guid Id,
  string Name,
  string AssemblyPath,
  string PluginTypeName,
  bool IsEnabled,
  string? ConfigurationJson,
  DateTimeOffset? LastLoadedAt,
  string? LoadedVersion,
  string? LoadedDescription,
  DateTimeOffset CreatedAt) : IHasPrimaryKey;

public record CreatePluginRequestDto(
  string Name,
  string AssemblyPath,
  string PluginTypeName,
  bool IsEnabled,
  string? ConfigurationJson);

public record UpdatePluginRequestDto(
  Guid Id,
  string Name,
  string AssemblyPath,
  string PluginTypeName,
  bool IsEnabled,
  string? ConfigurationJson);
