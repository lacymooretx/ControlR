namespace ControlR.Web.Server.Services;

public interface IAgentVersionProvider
{
  Task<Result<Version>> TryGetAgentVersion();
}

public class AgentVersionProvider(
  IWebHostEnvironment webHostEnvironment,
  ILogger<AgentVersionProvider> logger) : IAgentVersionProvider
{
  public async Task<Result<Version>> TryGetAgentVersion()
  {
    try
    {
      if (!webHostEnvironment.IsProduction())
      {
        var assemblyVersion = typeof(AgentVersionProvider).Assembly.GetName()?.Version;
        return assemblyVersion is not null
          ? Result.Ok(assemblyVersion)
          : Result.Fail<Version>("Assembly version not available.");
      }

      var physicalPath = Path.Combine(webHostEnvironment.WebRootPath, "downloads", "Version.txt");

      if (!File.Exists(physicalPath))
      {
        logger.LogError("Agent version file not found at path: {Path}", physicalPath);
        return Result.Fail<Version>("Version file not found.");
      }

      var versionString = await File.ReadAllTextAsync(physicalPath);

      if (!Version.TryParse(versionString?.Trim(), out var version))
      {
        logger.LogError("Invalid version format in file: {VersionString}", versionString);
        return Result.Fail<Version>("Invalid version format.");
      }

      return Result.Ok(version);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error retrieving agent version.");
      return Result.Fail<Version>("Error retrieving agent version.");
    }
  }
}