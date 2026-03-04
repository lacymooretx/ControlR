using System.Diagnostics;
using System.Text;
using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Hubs;
using ControlR.Libraries.Signalr.Client.Extensions;

namespace ControlR.Agent.Common.Services;

public interface IScriptRunner
{
  Task ExecuteScript(ScriptExecutionRequestHubDto request);
}

internal class ScriptRunner(
  IHubConnection<IAgentHub> hubConnection,
  ILogger<ScriptRunner> logger) : IScriptRunner
{
  private readonly IHubConnection<IAgentHub> _hubConnection = hubConnection;
  private readonly ILogger<ScriptRunner> _logger = logger;

  public async Task ExecuteScript(ScriptExecutionRequestHubDto request)
  {
    var stdout = new StringBuilder();
    var stderr = new StringBuilder();
    var exitCode = -1;
    var status = "Failed";

    try
    {
      _logger.LogInformation(
        "Executing script. Execution: {ExecutionId}, Result: {ResultId}, Type: {ScriptType}",
        request.ExecutionId,
        request.ResultId,
        request.ScriptType);

      var (fileName, arguments, tempFilePath) = PrepareScriptExecution(request.ScriptType, request.ScriptContent);

      try
      {
        var psi = new ProcessStartInfo
        {
          CreateNoWindow = true,
          FileName = fileName,
          Arguments = arguments,
          RedirectStandardError = true,
          RedirectStandardOutput = true,
          UseShellExecute = false,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await process.WaitForExitAsync(cts.Token);

        stdout.Append(await stdoutTask);
        stderr.Append(await stderrTask);
        exitCode = process.ExitCode;
        status = "Completed";
      }
      catch (OperationCanceledException)
      {
        status = "TimedOut";
        stderr.Append("Script execution timed out after 5 minutes.");
        _logger.LogWarning(
          "Script execution timed out. Execution: {ExecutionId}, Result: {ResultId}",
          request.ExecutionId,
          request.ResultId);
      }
      finally
      {
        if (tempFilePath is not null && File.Exists(tempFilePath))
        {
          try
          {
            File.Delete(tempFilePath);
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Failed to delete temp script file: {Path}", tempFilePath);
          }
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error executing script. Execution: {ExecutionId}", request.ExecutionId);
      stderr.Append($"Error: {ex.Message}");
    }

    // Report result back to server
    var resultDto = new ScriptExecutionResultHubDto(
      request.ExecutionId,
      request.ResultId,
      Guid.Empty,
      exitCode,
      stdout.ToString(),
      stderr.ToString(),
      status);

    try
    {
      await _hubConnection.Server.ReportScriptResult(resultDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to report script result. Execution: {ExecutionId}", request.ExecutionId);
    }
  }

  private static (string FileName, string Arguments, string? TempFilePath) PrepareScriptExecution(
    string scriptType,
    string scriptContent)
  {
    string tempFilePath;

    if (OperatingSystem.IsWindows())
    {
      switch (scriptType.ToLowerInvariant())
      {
        case "powershell":
          tempFilePath = Path.Combine(Path.GetTempPath(), $"controlr_{Guid.NewGuid():N}.ps1");
          File.WriteAllText(tempFilePath, scriptContent);
          return ("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFilePath}\"", tempFilePath);

        case "cmd":
          tempFilePath = Path.Combine(Path.GetTempPath(), $"controlr_{Guid.NewGuid():N}.cmd");
          File.WriteAllText(tempFilePath, scriptContent);
          return ("cmd.exe", $"/c \"{tempFilePath}\"", tempFilePath);

        default:
          tempFilePath = Path.Combine(Path.GetTempPath(), $"controlr_{Guid.NewGuid():N}.ps1");
          File.WriteAllText(tempFilePath, scriptContent);
          return ("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFilePath}\"", tempFilePath);
      }
    }
    else
    {
      tempFilePath = Path.Combine(Path.GetTempPath(), $"controlr_{Guid.NewGuid():N}.sh");
      File.WriteAllText(tempFilePath, scriptContent);

      // Make executable
      var chmodPsi = new ProcessStartInfo
      {
        FileName = "chmod",
        Arguments = $"+x \"{tempFilePath}\"",
        CreateNoWindow = true,
        UseShellExecute = false,
      };
      using var chmodProcess = Process.Start(chmodPsi);
      chmodProcess?.WaitForExit(5000);

      return ("/bin/bash", $"\"{tempFilePath}\"", tempFilePath);
    }
  }
}
