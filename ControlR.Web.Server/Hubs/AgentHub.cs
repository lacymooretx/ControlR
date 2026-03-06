using System.Runtime.CompilerServices;
using System.Threading.Channels;
using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Hubs.Clients;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.SignalR;
using ControlR.Web.Server.Services.DeviceManagement;

namespace ControlR.Web.Server.Hubs;

public class AgentHub(
  AppDb appDb,
  TimeProvider timeProvider,
  IHubContext<ViewerHub, IViewerHubClient> viewerHub,
  IDeviceManager deviceManager,
  IOptions<AppOptions> appOptions,
  IOutputCacheStore outputCacheStore,
  IHubStreamStore hubStreamStore,
  IAgentVersionProvider agentVersionProvider,
  IMetricsIngestionService metricsIngestionService,
  ILogger<AgentHub> logger) : HubWithItems<IAgentHubClient>, IAgentHub
{
  private readonly IAgentVersionProvider _agentVersionProvider = agentVersionProvider;
  private readonly AppDb _appDb = appDb;
  private readonly IOptions<AppOptions> _appOptions = appOptions;
  private readonly IDeviceManager _deviceManager = deviceManager;
  private readonly IHubStreamStore _hubStreamStore = hubStreamStore;
  private readonly ILogger<AgentHub> _logger = logger;
  private readonly IMetricsIngestionService _metricsIngestionService = metricsIngestionService;
  private readonly IOutputCacheStore _outputCacheStore = outputCacheStore;
  private readonly TimeProvider _timeProvider = timeProvider;
  private readonly IHubContext<ViewerHub, IViewerHubClient> _viewerHub = viewerHub;

  private DeviceResponseDto? Device
  {
    get => GetItem<DeviceResponseDto?>(null);
    set => SetItem(value);
  }

  public ChannelReader<byte[]> GetFileStreamFromViewer(FileUploadHubDto dto)
  {
    if (!_hubStreamStore.TryGet<byte[]>(dto.StreamId, out var signaler))
    {
      _logger.LogWarning("No signaler found for file upload stream ID: {StreamId}", dto.StreamId);
      var errorChannel = Channel.CreateUnbounded<byte[]>();
      errorChannel.Writer.TryComplete(new InvalidOperationException("No signaler found for stream."));
      return errorChannel.Reader;
    }

    _logger.LogInformation("Agent is starting to read file upload stream for: {FileName}", dto.FileName);

    // Create a background task to log completion
    _ = Task.Run(async () =>
    {
      try
      {
        await signaler.Reader.Completion;
        _logger.LogInformation("Agent has finished reading file upload stream for: {FileName}", dto.FileName);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error reading file upload stream for: {FileName}", dto.FileName);
      }
    });

    return signaler.Reader;
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    try
    {
      if (Device is { } cachedDeviceDto)
      {
        // Check if this is still the current connection for this device
        var deviceConnectionId = await _appDb.Devices
          .Where(d => d.Id == cachedDeviceDto.Id)
          .Select(d => d.ConnectionId)
          .FirstOrDefaultAsync();

        // Only mark offline if this was the current connection
        if (deviceConnectionId == Context.ConnectionId)
        {
          var updateResult = await _deviceManager.MarkDeviceOffline(cachedDeviceDto.Id, _timeProvider.GetLocalNow());
          if (updateResult.IsSuccess)
          {
            var offlineDto = cachedDeviceDto with
            {
              IsOnline = false,
              LastSeen = _timeProvider.GetLocalNow()
            };
            await SendDeviceUpdate(updateResult.Value, offlineDto);
          }
        }
        else
        {
          _logger.LogDebug(
            "Skipping offline update. Device has reconnected with connection {CurrentConnectionId}. Disconnecting {OldConnectionId}.",
            deviceConnectionId, Context.ConnectionId);
        }
      }

      await base.OnDisconnectedAsync(exception);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during device disconnect.");
    }
  }

  public async Task ReportInventory(InventoryReportHubDto report)
  {
    try
    {
      if (Device is null)
      {
        _logger.LogWarning("ReportInventory called but Device is null.");
        return;
      }

      var device = await _appDb.Devices
        .FirstOrDefaultAsync(d => d.Id == report.DeviceId);

      if (device is null)
      {
        _logger.LogWarning("Device {DeviceId} not found for inventory report.", report.DeviceId);
        return;
      }

      // Update hardware info
      device.BiosVersion = report.Hardware.BiosVersion;
      device.LastInventoryScan = DateTimeOffset.UtcNow;
      device.Manufacturer = report.Hardware.Manufacturer;
      device.Model = report.Hardware.Model;
      device.SerialNumber = report.Hardware.SerialNumber;

      // Replace software inventory
      var existingSoftware = await _appDb.SoftwareInventoryItems
        .Where(s => s.DeviceId == report.DeviceId)
        .ToListAsync();
      _appDb.SoftwareInventoryItems.RemoveRange(existingSoftware);

      var now = DateTimeOffset.UtcNow;
      var softwareItems = report.Software.Select(s => new SoftwareInventoryItem
      {
        DeviceId = report.DeviceId,
        InstallDate = s.InstallDate,
        LastReportedAt = now,
        Name = s.Name,
        Publisher = s.Publisher,
        TenantId = device.TenantId,
        Version = s.Version,
      });
      await _appDb.SoftwareInventoryItems.AddRangeAsync(softwareItems);

      // Replace installed updates
      var existingUpdates = await _appDb.InstalledUpdates
        .Where(u => u.DeviceId == report.DeviceId)
        .ToListAsync();
      _appDb.InstalledUpdates.RemoveRange(existingUpdates);

      var updateItems = report.Updates.Select(u => new InstalledUpdate
      {
        DeviceId = report.DeviceId,
        InstalledOn = u.InstalledOn,
        LastReportedAt = now,
        TenantId = device.TenantId,
        Title = u.Title,
        UpdateId = u.UpdateId,
      });
      await _appDb.InstalledUpdates.AddRangeAsync(updateItems);

      await _appDb.SaveChangesAsync();

      _logger.LogInformation(
        "Inventory report received for device {DeviceName}: {SoftwareCount} software, {UpdateCount} updates.",
        device.Name, report.Software.Count, report.Updates.Count);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing inventory report.");
    }
  }

  public async Task ReportScriptResult(ScriptExecutionResultHubDto result)
  {
    try
    {
      var executionResult = await _appDb.ScriptExecutionResults
        .Include(x => x.ScriptExecution)
        .FirstOrDefaultAsync(x => x.Id == result.ResultId);

      if (executionResult is null)
      {
        _logger.LogWarning("Script execution result {ResultId} not found.", result.ResultId);
        return;
      }

      executionResult.CompletedAt = DateTimeOffset.UtcNow;
      executionResult.ExitCode = result.ExitCode;
      executionResult.StandardError = result.StandardError;
      executionResult.StandardOutput = result.StandardOutput;
      executionResult.StartedAt ??= DateTimeOffset.UtcNow;
      executionResult.Status = result.Status;

      // Check if all results for this execution are complete
      var execution = executionResult.ScriptExecution;
      if (execution is not null)
      {
        var allResults = await _appDb.ScriptExecutionResults
          .Where(x => x.ScriptExecutionId == execution.Id)
          .ToListAsync();

        var allComplete = allResults.All(r => r.Status is "Completed" or "Failed" or "TimedOut");
        if (allComplete)
        {
          execution.CompletedAt = DateTimeOffset.UtcNow;
          execution.Status = allResults.Any(r => r.Status == "Failed") ? "CompletedWithErrors" : "Completed";
        }
      }

      await _appDb.SaveChangesAsync();

      // Forward progress to viewers in this tenant
      if (Device is { TenantId: var tenantId })
      {
        await _viewerHub.Clients
          .Group(HubGroupNames.GetUserRoleGroupName(RoleNames.TenantAdministrator, tenantId))
          .ReceiveScriptExecutionProgress(result);

        await _viewerHub.Clients
          .Group(HubGroupNames.GetUserRoleGroupName(RoleNames.DeviceSuperUser, tenantId))
          .ReceiveScriptExecutionProgress(result);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while processing script execution result.");
    }
  }

  public async Task ReportPatchScanResult(PatchScanResultHubDto result)
  {
    try
    {
      if (Device is null)
      {
        _logger.LogWarning("ReportPatchScanResult called but Device is null.");
        return;
      }

      var device = await _appDb.Devices
        .AsNoTracking()
        .FirstOrDefaultAsync(d => d.Id == result.DeviceId);

      if (device is null)
      {
        _logger.LogWarning("Device {DeviceId} not found for patch scan result.", result.DeviceId);
        return;
      }

      var now = DateTimeOffset.UtcNow;

      // Remove existing pending patches for this device that are still in Pending status
      var existingPending = await _appDb.PendingPatches
        .Where(p => p.DeviceId == result.DeviceId && p.Status == "Pending")
        .ToListAsync();
      _appDb.PendingPatches.RemoveRange(existingPending);

      // Add newly detected patches
      foreach (var patch in result.AvailablePatches)
      {
        var pendingPatch = new PendingPatch
        {
          DeviceId = result.DeviceId,
          UpdateId = patch.UpdateId,
          Title = patch.Title,
          Description = patch.Description,
          IsImportant = patch.IsImportant,
          IsCritical = patch.IsCritical,
          SizeBytes = patch.SizeBytes,
          DetectedAt = now,
          Status = "Pending",
          TenantId = device.TenantId,
        };
        await _appDb.PendingPatches.AddAsync(pendingPatch);
      }

      await _appDb.SaveChangesAsync();

      _logger.LogInformation(
        "Patch scan result received for device {DeviceName}: {PatchCount} available patches.",
        device.Name, result.AvailablePatches.Length);

      // Forward to viewers
      if (Device is { TenantId: var tenantId })
      {
        await _viewerHub.Clients
          .Group(HubGroupNames.GetUserRoleGroupName(RoleNames.TenantAdministrator, tenantId))
          .ReceivePatchScanProgress(result);

        await _viewerHub.Clients
          .Group(HubGroupNames.GetUserRoleGroupName(RoleNames.DeviceSuperUser, tenantId))
          .ReceivePatchScanProgress(result);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing patch scan result.");
    }
  }

  public async Task ReportPatchInstallResult(PatchInstallResultHubDto result)
  {
    try
    {
      if (Device is null)
      {
        _logger.LogWarning("ReportPatchInstallResult called but Device is null.");
        return;
      }

      // Find the most recent in-progress installation for this device
      var installation = await _appDb.PatchInstallations
        .Where(i => i.DeviceId == result.DeviceId && i.Status == "InProgress")
        .OrderByDescending(i => i.InitiatedAt)
        .FirstOrDefaultAsync();

      if (installation is not null)
      {
        installation.CompletedAt = DateTimeOffset.UtcNow;
        installation.InstalledCount = result.InstalledCount;
        installation.FailedCount = result.FailedCount;
        installation.Status = result.IsSuccess ? "Completed" : "CompletedWithErrors";
      }

      // Mark successfully installed patches
      if (result.InstalledCount > 0)
      {
        var pendingPatches = await _appDb.PendingPatches
          .Where(p => p.DeviceId == result.DeviceId && p.Status == "Pending")
          .ToListAsync();

        // We don't know exactly which patches were installed from the result alone,
        // so mark them based on a subsequent scan. For now, just save the installation result.
      }

      await _appDb.SaveChangesAsync();

      _logger.LogInformation(
        "Patch install result received for device {DeviceId}: Success={IsSuccess}, Installed={Installed}, Failed={Failed}",
        result.DeviceId, result.IsSuccess, result.InstalledCount, result.FailedCount);

      // Forward to viewers
      if (Device is { TenantId: var tenantId })
      {
        await _viewerHub.Clients
          .Group(HubGroupNames.GetUserRoleGroupName(RoleNames.TenantAdministrator, tenantId))
          .ReceivePatchInstallProgress(result);

        await _viewerHub.Clients
          .Group(HubGroupNames.GetUserRoleGroupName(RoleNames.DeviceSuperUser, tenantId))
          .ReceivePatchInstallProgress(result);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing patch install result.");
    }
  }

  public async Task<bool> SendChatResponse(ChatResponseHubDto responseDto)
  {
    try
    {
      _logger.LogInformation(
        "Sending chat response to viewer {ViewerConnectionId} for session {SessionId}",
        responseDto.ViewerConnectionId,
        responseDto.SessionId);

      return await _viewerHub.Clients
        .Client(responseDto.ViewerConnectionId)
        .ReceiveChatResponse(responseDto);
    }
    catch (IOException ex) when (ex.Message.Contains("does not exist"))
    {
      _logger.LogWarning(
        "Viewer {ViewerConnectionId} for chat session {SessionId} is no longer connected.",
        responseDto.ViewerConnectionId,
        responseDto.SessionId);
      await Clients.Caller.CloseChatSession(responseDto.SessionId, responseDto.DesktopSessionProcessId);
      return false;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error forwarding chat response to viewer.");
      return false;
    }
  }

  public async Task SendDesktopPreviewStream(Guid streamId, ChannelReader<byte[]> jpegChunks)
  {
    try
    {
      _logger.LogInformation("Receiving desktop preview stream for stream ID: {StreamId}", streamId);

      await ProcessAgentStream(
        streamId,
        jpegChunks,
        async signaler => await signaler.WriteFromChannelReader(jpegChunks, Context.ConnectionAborted),
        "desktop preview");

      _logger.LogInformation("Desktop preview stream completed for stream ID: {StreamId}", streamId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while receiving desktop preview stream for stream ID: {StreamId}", streamId);
    }
  }

  public async Task SendDirectoryContentsStream(Guid streamId, bool directoryExists,
    ChannelReader<FileSystemEntryDto[]> entryChunks)
  {
    try
    {
      await ProcessAgentStream(
        streamId,
        entryChunks,
        async signaler =>
        {
          signaler.Metadata = directoryExists;
          await signaler.WriteFromChannelReader(entryChunks, Context.ConnectionAborted);
        },
        "directory contents");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error handling directory contents stream {StreamId}", streamId);
    }
  }

  public async Task<Result> SendFileContentStream(Guid streamId, ChannelReader<byte[]> stream)
  {
    try
    {
      _logger.LogInformation("Setting file download stream for stream ID: {StreamId}", streamId);

      await ProcessAgentStream(
        streamId,
        stream,
        async signaler => await signaler.WriteFromChannelReader(stream, Context.ConnectionAborted),
        "file download");

      _logger.LogInformation("File download stream completed for stream ID: {StreamId}", streamId);
      return Result.Ok();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error handling file download stream {StreamId}", streamId);
      return Result.Fail("An error occurred while handling the file download stream.");
    }
  }

  public async Task SendSubdirectoriesStream(Guid streamId, ChannelReader<FileSystemEntryDto[]> subdirectoryChunks)
  {
    try
    {
      await ProcessAgentStream(
        streamId,
        subdirectoryChunks,
        async signaler => await signaler.WriteFromChannelReader(subdirectoryChunks, Context.ConnectionAborted),
        "subdirectories");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error handling subdirectories stream {StreamId}", streamId);
      throw;
    }
  }

  public async Task SendPtyOutputToViewer(string viewerConnectionId, PtyOutputDto outputDto)
  {
    try
    {
      await _viewerHub.Clients
        .Client(viewerConnectionId)
        .ReceivePtyOutput(outputDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending PTY output to viewer.");
    }
  }

  public async Task SendTerminalOutputToViewer(string viewerConnectionId, TerminalOutputDto outputDto)
  {
    try
    {
      await _viewerHub.Clients
        .Client(viewerConnectionId)
        .ReceiveTerminalOutput(outputDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending terminal output to viewer.");
    }
  }

  public async Task<Result<DeviceResponseDto>> UpdateDevice(DeviceUpdateRequestDto agentDto)
  {
    try
    {
      // Allow agents to self-bootstrap when enabled
      if (_appOptions.Value.AllowAgentsToSelfBootstrap && agentDto.TenantId == Guid.Empty)
      {
        var lastTenant = await _appDb.Tenants
          .OrderByDescending(x => x.CreatedAt)
          .FirstOrDefaultAsync();

        if (lastTenant is null)
        {
          return Result.Fail<DeviceResponseDto>("No tenants found.");
        }

        // Update the DTO with the assigned TenantId
        agentDto = agentDto with { TenantId = lastTenant.Id };
      }

      if (agentDto.TenantId == Guid.Empty)
      {
        return Result.Fail<DeviceResponseDto>("Invalid tenant ID.");
      }

      if (!await _appDb.Tenants.AnyAsync(x => x.Id == agentDto.TenantId))
      {
        return Result.Fail<DeviceResponseDto>("Invalid tenant ID.");
      }

      var remoteIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;
      var connectionContext = new DeviceConnectionContext(
        ConnectionId: Context.ConnectionId,
        RemoteIpAddress: remoteIp,
        LastSeen: _timeProvider.GetLocalNow(),
        IsOnline: true
      );

      var updateResult = await UpdateDeviceEntity(agentDto, connectionContext);

      if (!updateResult.IsSuccess)
      {
        return Result.Fail<DeviceResponseDto>(updateResult.Reason);
      }

      var deviceEntity = updateResult.Value;
      await AddToGroups(deviceEntity);

      var isOutdated = await GetIsAgentOutdated(deviceEntity);
      Device = deviceEntity.ToDto(isOutdated);

      await SendDeviceUpdate(deviceEntity, Device);

      // Record metrics snapshot for monitoring
      var memoryPercent = deviceEntity.TotalMemory > 0
        ? (deviceEntity.UsedMemory / deviceEntity.TotalMemory) * 100
        : 0;
      var diskPercent = deviceEntity.TotalStorage > 0
        ? (deviceEntity.UsedStorage / deviceEntity.TotalStorage) * 100
        : 0;
      _metricsIngestionService.RecordSnapshot(
        deviceEntity.Id,
        deviceEntity.TenantId,
        deviceEntity.CpuUtilization,
        memoryPercent,
        diskPercent);

      return Result.Ok(Device);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while updating device.");
      return Result.Fail<DeviceResponseDto>("An error occurred while updating the device.");
    }
  }

  private static async Task DrainChannelReader<T>(ChannelReader<T> reader)
  {
    try
    {
      // Consume any remaining items in the channel to prevent SignalR streaming errors
      await foreach (var _ in reader.ReadAllAsync())
      {
        // Discard the data
      }
    }
    catch
    {
      // Ignore errors while draining
    }
  }

  private async Task AddToGroups(Device deviceEntity)
  {
    if (Device is not null)
    {
      return;
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.GetTenantDevicesGroupName(deviceEntity.TenantId));
    await Groups.AddToGroupAsync(Context.ConnectionId,
      HubGroupNames.GetDeviceGroupName(deviceEntity.Id, deviceEntity.TenantId));

    if (deviceEntity.Tags is { Count: > 0 } tags)
    {
      foreach (var tag in tags)
      {
        await Groups.AddToGroupAsync(Context.ConnectionId,
          HubGroupNames.GetTagGroupName(tag.Id, deviceEntity.TenantId));
      }
    }
  }

  private async Task<bool> GetIsAgentOutdated(Device deviceEntity)
  {
    var agentVersionResult = await _agentVersionProvider.TryGetAgentVersion();
    if (!agentVersionResult.IsSuccess)
    {
      return false;
    }

    if (!Version.TryParse(deviceEntity.AgentVersion, out var deviceVersion))
    {
      return false;
    }

    var currentAgentVersion = agentVersionResult.Value;
    return deviceVersion != currentAgentVersion;
  }

  /// <summary>
  ///   Safely processes a streaming request by writing from an agent's ChannelReader to a signaler.
  ///   Automatically drains the channel on any error or cancellation to prevent SignalR connection breaks.
  /// </summary>
  private async Task ProcessAgentStream<T>(
    Guid streamId,
    ChannelReader<T> agentStream,
    Func<HubStreamSignaler<T>, Task> processSignaler,
    string streamType,
    [CallerMemberName] string callerName = "")
  {
    try
    {
      if (!_hubStreamStore.TryGet<T>(streamId, out var signaler))
      {
        _logger.LogWarning("No signaler found for {StreamType} stream ID: {StreamId}", streamType, streamId);
        await DrainChannelReader(agentStream);
        throw new InvalidOperationException($"No signaler found for {streamType} stream.");
      }
      await processSignaler.Invoke(signaler);
    }
    catch (OperationCanceledException)
    {
      _logger.LogInformation("{StreamType} stream {StreamId} was canceled in method {CallerName}. ",
        streamType, streamId, callerName);

      await DrainChannelReader(agentStream);
    }
    catch (Exception)
    {
      await DrainChannelReader(agentStream);
      throw;
    }
  }

  private async Task SendDeviceUpdate(Device device, DeviceResponseDto dto)
  {
    await _viewerHub.Clients
      .Group(HubGroupNames.GetUserRoleGroupName(RoleNames.DeviceSuperUser, device.TenantId))
      .ReceiveDeviceUpdate(dto);

    // Invalidate the device grid cache using the extension method
    await _outputCacheStore.InvalidateDeviceCacheAsync(device.Id);
    _logger.LogDebug("Invalidated device grid cache after device update: {DeviceId}", device.Id);

    if (device.Tags is null)
    {
      return;
    }

    var groupNames = device.Tags.Select(x => HubGroupNames.GetTagGroupName(x.Id, x.TenantId));
    await _viewerHub.Clients.Groups(groupNames).ReceiveDeviceUpdate(dto);
  }

  private async Task<Result<Device>> UpdateDeviceEntity(
    DeviceUpdateRequestDto agentDto,
    DeviceConnectionContext context)
  {
    // Allow agents to self-bootstrap when enabled
    if (_appOptions.Value.AllowAgentsToSelfBootstrap)
    {
      var device = await _deviceManager.AddOrUpdate(agentDto, context);
      return Result.Ok(device);
    }

    return await _deviceManager.UpdateDevice(agentDto, context);
  }
}