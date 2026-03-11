using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Channels;
using ControlR.Libraries.Shared.Constants;
using ControlR.Libraries.Shared.Dtos.Devices;
using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Dtos.HubDtos.PwshCommandCompletions;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using ControlR.Libraries.Shared.Enums;
using ControlR.Libraries.Shared.Helpers;
using ControlR.Libraries.Shared.Hubs.Clients;
using ControlR.Web.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace ControlR.Web.Server.Hubs;

[Authorize]
public class ViewerHub(
  UserManager<AppUser> userManager,
  AppDb appDb,
  IAuthorizationService authorizationService,
  IHubContext<AgentHub, IAgentHubClient> agentHub,
  IHubStreamStore hubStreamStore,
  IOptionsMonitor<AppOptions> appOptions,
  IAuditService auditService,
  IActionVerificationService actionVerificationService,
  ILogger<ViewerHub> logger)
  : HubWithItems<IViewerHubClient>, IViewerHub
{
  private readonly IActionVerificationService _actionVerificationService = actionVerificationService;
  private readonly IHubContext<AgentHub, IAgentHubClient> _agentHub = agentHub;
  private readonly AppDb _appDb = appDb;
  private readonly IOptionsMonitor<AppOptions> _appOptions = appOptions;
  private readonly IAuditService _auditService = auditService;
  private readonly IAuthorizationService _authorizationService = authorizationService;
  private readonly IHubStreamStore _hubStreamStore = hubStreamStore;
  private readonly ILogger<ViewerHub> _logger = logger;
  private readonly UserManager<AppUser> _userManager = userManager;

  public async Task<Result> CloseChatSession(Guid deviceId, Guid sessionId, int targetProcessId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      _logger.LogInformation(
        "Closing chat session {SessionId} for device {DeviceId} and process {ProcessId}",
        sessionId,
        deviceId,
        targetProcessId);

      return await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .CloseChatSession(sessionId, targetProcessId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while closing chat session {SessionId} on device {DeviceId}.", sessionId, deviceId);
      return Result.Fail("Agent could not be reached.");
    }
  }

  public async Task ClosePtySession(Guid deviceId, Guid terminalSessionId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return;
      }

      await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .ClosePtySession(terminalSessionId);

      AuditHubAction(AuditEventTypes.Terminal, AuditActions.End, authResult.Value, terminalSessionId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while closing PTY session.");
    }
  }

  public async Task CloseTerminalSession(Guid deviceId, Guid terminalSessionId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return;
      }

      await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .CloseTerminalSession(terminalSessionId);

      AuditHubAction(AuditEventTypes.Terminal, AuditActions.End, authResult.Value, terminalSessionId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while closing terminal session.");
    }
  }

  public async Task<Result> CreatePtySession(Guid deviceId, Guid terminalSessionId, int cols, int rows)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Forbidden.");
      }

      var result = await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .CreatePtySession(terminalSessionId, Context.ConnectionId, cols, rows);

      if (result.IsSuccess)
      {
        AuditHubAction(AuditEventTypes.Terminal, AuditActions.Start, authResult.Value, terminalSessionId);
      }

      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while creating PTY session.");
      return Result.Fail("An error occurred.");
    }
  }

  public async Task<Result> CreateTerminalSession(
    Guid deviceId,
    Guid terminalSessionId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Forbidden.");
      }

      var result = await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .CreateTerminalSession(terminalSessionId, Context.ConnectionId);

      if (result.IsSuccess)
      {
        AuditHubAction(AuditEventTypes.Terminal, AuditActions.Start, authResult.Value, terminalSessionId);
      }

      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while creating terminal session.");
      return Result.Fail("An error occurred.");
    }
  }

  public async Task<Result<ScriptExecutionDto>> ExecuteScript(ExecuteScriptRequestDto request)
  {
    try
    {
      if (!IsActionVerified())
      {
        return Result.Fail<ScriptExecutionDto>("Action verification required. Please verify your identity first.");
      }

      if (!TryGetUserId(out var userId))
      {
        return Result.Fail<ScriptExecutionDto>("Failed to get user ID.");
      }

      if (!TryGetTenantId(out var tenantId))
      {
        return Result.Fail<ScriptExecutionDto>("Failed to get tenant ID.");
      }

      var isClientUser = Context.User?.IsInRole(RoleNames.ClientUser) ?? false;

      // Resolve script content and type
      string scriptContent;
      string scriptType;
      Guid? scriptId = null;

      if (request.ScriptId is not null)
      {
        var script = await _appDb.SavedScripts
          .AsNoTracking()
          .FirstOrDefaultAsync(x => x.Id == request.ScriptId);

        if (script is null)
        {
          return Result.Fail<ScriptExecutionDto>("Script not found.");
        }

        if (isClientUser && !script.IsPublishedToClients)
        {
          return Result.Fail<ScriptExecutionDto>("Script not available.");
        }

        scriptContent = script.ScriptContent;
        scriptType = script.ScriptType;
        scriptId = script.Id;
      }
      else
      {
        if (isClientUser)
        {
          return Result.Fail<ScriptExecutionDto>("Ad-hoc scripts are not allowed for client users.");
        }

        if (string.IsNullOrWhiteSpace(request.AdHocScriptContent))
        {
          return Result.Fail<ScriptExecutionDto>("Script content is required.");
        }

        scriptContent = request.AdHocScriptContent;
        scriptType = request.ScriptType;
      }

      // Authorize against each target device
      var authorizedDevices = new List<Device>();
      foreach (var deviceId in request.TargetDeviceIds)
      {
        if (await TryAuthorizeAgainstDevice(deviceId) is { IsSuccess: true } authResult)
        {
          authorizedDevices.Add(authResult.Value);
        }
      }

      if (authorizedDevices.Count == 0)
      {
        return Result.Fail<ScriptExecutionDto>("No authorized devices found.");
      }

      // Create execution record
      var execution = new ScriptExecution
      {
        AdHocScriptContent = request.ScriptId is null ? scriptContent : null,
        InitiatedByUserId = userId,
        ScriptId = scriptId,
        ScriptType = scriptType,
        StartedAt = DateTimeOffset.UtcNow,
        Status = "Running",
        TenantId = tenantId,
      };

      await _appDb.ScriptExecutions.AddAsync(execution);

      // Create result records for each device
      var results = new List<ScriptExecutionResult>();
      foreach (var device in authorizedDevices)
      {
        var result = new ScriptExecutionResult
        {
          DeviceId = device.Id,
          DeviceName = device.Name,
          ScriptExecutionId = execution.Id,
          Status = "Pending",
          TenantId = tenantId,
        };
        results.Add(result);
      }

      await _appDb.ScriptExecutionResults.AddRangeAsync(results);
      await _appDb.SaveChangesAsync();

      // Fan out to agents
      foreach (var device in authorizedDevices)
      {
        var resultId = results.First(r => r.DeviceId == device.Id).Id;
        var hubDto = new ScriptExecutionRequestHubDto(
          execution.Id,
          resultId,
          scriptContent,
          scriptType);

        try
        {
          await _agentHub.Clients
            .Client(device.ConnectionId)
            .ExecuteScript(hubDto);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Failed to send script to device {DeviceId}.", device.Id);
        }
      }

      // Reload with nav properties for DTO
      var savedExecution = await _appDb.ScriptExecutions
        .AsNoTracking()
        .Include(x => x.Script)
        .Include(x => x.Results)
        .FirstAsync(x => x.Id == execution.Id);

      AuditHubAction(
        AuditEventTypes.ScriptExecution,
        AuditActions.Execute,
        authorizedDevices.First(),
        details: $"Execution: {execution.Id}, Devices: {authorizedDevices.Count}");

      return Result.Ok(savedExecution.ToDto());
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while executing script.");
      return Result.Fail<ScriptExecutionDto>("An error occurred while executing the script.");
    }
  }

  public async Task<DesktopSession[]> GetActiveDesktopSessions(Guid deviceId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return [];
      }

      var device = authResult.Value;
      return await _agentHub.Clients.Client(device.ConnectionId).GetActiveDesktopSessions();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while getting Windows sessions from agent.");
      return [];
    }
  }

  public async Task<Result<PwshCompletionsResponseDto>> GetPwshCompletions(PwshCompletionsRequestDto request)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(request.DeviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail<PwshCompletionsResponseDto>("Forbidden.");
      }

      // Create a new request with ViewerConnectionId
      var requestWithViewerConnection = request with { ViewerConnectionId = Context.ConnectionId };

      return await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .GetPwshCompletions(requestWithViewerConnection);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while getting PowerShell command completions.");
      return Result.Fail<PwshCompletionsResponseDto>("An error occurred.");
    }
  }

  public async Task<Result> InvokeCtrlAltDel(Guid deviceId, int targetDesktopProcessId, DesktopSessionType desktopSessionType)
  {
    try
    {
      _logger.LogInformation(
        "Invoking CtrlAltDel for device {DeviceId} and process {ProcessId}.  User: {UserId}", 
        deviceId,
        targetDesktopProcessId,
        Context.UserIdentifier);

      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      if (!TryGetUserId(out var userId))
      {
        _logger.LogError("Failed to get user ID for CtrlAltDel invocation.");
        return Result.Fail("Failed to get user ID.");
      }

      var displayNameResult = await GetDisplayName(userId);
      if (!displayNameResult.IsSuccess)
      {
        return displayNameResult.ToResult();
      }

      var dto = new InvokeCtrlAltDelRequestDto(
        targetDesktopProcessId, 
        Context.User?.Identity?.Name ?? "Unknown",
        desktopSessionType);

      return await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .InvokeCtrlAltDel(dto);
    }
    catch (Exception ex)
    {
      return Result
        .Fail(ex, "An error occurred while invoking CtrlAltDel.")
        .Log(_logger);
    }
  }

  public override async Task OnConnectedAsync()
  {
    try
    {
      await base.OnConnectedAsync();

      if (Context.User?.TryGetUserId(out var userId) != true)
      {
        _logger.LogCritical("User is null.  Authorize tag should have prevented this.");
        return;
      }

      if (!Context.User.TryGetTenantId(out var tenantId))
      {
        _logger.LogCritical("Failed to get tenant ID.");
        return;
      }

      var user = await _appDb.Users
        .Include(x => x.Tags)
        .FirstOrDefaultAsync(x => x.Id == userId);

      if (user is null)
      {
        _logger.LogCritical("Failed to find user from UserManager.");
        return;
      }

      user.IsOnline = true;
      await _appDb.SaveChangesAsync();

      if (Context.User.IsInRole(RoleNames.ServerAdministrator))
      {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroupNames.ServerAdministrators);
      }

      if (Context.User.IsInRole(RoleNames.TenantAdministrator))
      {
        await Groups.AddToGroupAsync(Context.ConnectionId,
          HubGroupNames.GetUserRoleGroupName(RoleNames.TenantAdministrator, tenantId));
      }

      if (Context.User.IsInRole(RoleNames.DeviceSuperUser))
      {
        await Groups.AddToGroupAsync(Context.ConnectionId,
          HubGroupNames.GetUserRoleGroupName(RoleNames.DeviceSuperUser, tenantId));
      }

      if (user.Tags is { Count: > 0 } tags)
      {
        foreach (var tag in tags)
        {
          await Groups.AddToGroupAsync(Context.ConnectionId,
            HubGroupNames.GetTagGroupName(tag.Id, tenantId));
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during viewer connect.");
    }
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    try
    {
      await base.OnDisconnectedAsync(exception);

      if (Context.User is null)
      {
        return;
      }

      var user = await _userManager.GetUserAsync(Context.User);

      if (user is null)
      {
        _logger.LogCritical("Failed to find user from UserManager.");
        return;
      }

      user.IsOnline = false;
      await _userManager.UpdateAsync(user);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during viewer disconnect.");
    }
  }

  public async Task RefreshDeviceInfo(Guid deviceId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return;
      }

      await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .RefreshDeviceInfo();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while refreshing device info.");
    }
  }

  public async Task<Result> RequestRemoteControlSession(
    Guid deviceId,
    RemoteControlSessionRequestDto sessionRequestDto)
  {
    try
    {
      if (Context.User is null)
      {
        return Result.Fail("User is null.");
      }

      if (!TryGetUserId(out var userId))
      {
        return Result.Fail("Failed to get user ID.");
      }

      var remoteIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();

      var displayNameResult = await GetDisplayName(userId);
      if (!displayNameResult.IsSuccess)
      {
        return displayNameResult.ToResult();
      }

      var displayName = displayNameResult.Value;

      _logger.LogInformation(
        "Starting streaming session requested by user {DisplayName} ({UserId}) for device {DeviceId} from IP {RemoteIp}.",
        displayName,
        userId,
        deviceId,
        remoteIp);

      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      var device = authResult.Value;

      var notifyUserSetting =
        _appDb.TenantSettings.FirstOrDefault(x => x.Name == TenantSettingsNames.NotifyUserOnSessionStart);
      if (notifyUserSetting is not null &&
          bool.TryParse(notifyUserSetting.Value, out var notifyUser))
      {
        sessionRequestDto = sessionRequestDto with { NotifyUserOnSessionStart = notifyUser };
      }

      sessionRequestDto = sessionRequestDto with
      {
        ViewerName = displayName,
        ViewerConnectionId = Context.ConnectionId
      };

      var result = await _agentHub.Clients
        .Client(device.ConnectionId)
        .CreateRemoteControlSession(sessionRequestDto);

      if (result.IsSuccess)
      {
        AuditHubAction(AuditEventTypes.RemoteControl, AuditActions.Start, device);
      }

      return result;
    }
    catch (Exception ex)
    {
      return Result.Fail(ex);
    }
  }

  public async Task<Result> RequestVncSession(Guid deviceId, VncSessionRequestDto sessionRequestDto)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      if (Context.User is null)
      {
        return Result.Fail("User is null.");
      }

      if (!TryGetUserId(out var userId))
      {
        return Result.Fail("Failed to get user ID.");
      }

      var user = await _userManager.Users
        .AsNoTracking()
        .Include(x => x.UserPreferences)
        .FirstOrDefaultAsync(x => x.Id == userId);

      if (user is null)
      {
        return Result.Fail("User not found.");
      }

      var displayName = user.UserPreferences
        ?.FirstOrDefault(x => x.Name == UserPreferenceNames.UserDisplayName)
        ?.Value;
      var remoteIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();

      _logger.LogInformation(
        "Starting VNC session requested by user {DisplayName} ({UserId}) for device {DeviceId} from IP {RemoteIp}.",
        displayName,
        userId,
        deviceId,
        remoteIp);

      var device = authResult.Value;

      if (string.IsNullOrWhiteSpace(displayName))
      {
        displayName = user.UserName ?? "";
      }

      sessionRequestDto = sessionRequestDto with 
      { 
        ViewerConnectionId = Context.ConnectionId,
        ViewerName = displayName,
      };

      return await _agentHub.Clients
        .Client(device.ConnectionId)
        .CreateVncSession(sessionRequestDto);
    }
    catch (Exception ex)
    {
      return Result.Fail(ex);
    }
  }

  public async Task SendAgentUpdateTrigger(Guid deviceId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return;
      }

      await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .ReceiveAgentUpdateTrigger();

      AuditHubAction(AuditEventTypes.AgentUpdate, AuditActions.Trigger, authResult.Value);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending agent update trigger.");
    }
  }

  public async Task<Result> SendChatMessage(Guid deviceId, ChatMessageHubDto dto)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      var user = await GetRequiredUser(q => q.Include(u => u.UserPreferences));
      var displayName = await GetDisplayName(user);

      // Log the chat message being sent
      _logger.LogInformation(
        "Chat message sent by user {SenderName} ({SenderEmail}) to device {DeviceId} for session {SessionId}",
        displayName,
        user.Email,
        deviceId,
        dto.SessionId);

      dto = dto with
      {
        ViewerConnectionId = Context.ConnectionId,
        SenderName = displayName,
        SenderEmail = $"{user.Email}"
      };

      var result = await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .SendChatMessage(dto);

      if (result.IsSuccess)
      {
        AuditHubAction(AuditEventTypes.Chat, AuditActions.Send, authResult.Value, dto.SessionId);
      }

      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending chat message to agent.");
      return Result.Fail("Agent could not be reached.");
    }
  }

  public async Task SendDtoToAgent(Guid deviceId, DtoWrapper wrapper)
  {
    try
    {
      using var scope = _logger.BeginMemberScope();

      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return;
      }

      await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .ReceiveDto(wrapper);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending DTO to agent.");
    }
  }

  public async Task SendDtoToUserGroups(DtoWrapper wrapper)
  {
    if (!TryGetUserId(out var userId) ||
        !TryGetTenantId(out var tenantId))
    {
      return;
    }

    if (Context.User!.IsInRole(RoleNames.DeviceSuperUser))
    {
      await _agentHub
        .Clients
        .Group(HubGroupNames.GetTenantDevicesGroupName(tenantId))
        .ReceiveDto(wrapper);
      return;
    }

    var user = await _userManager
      .Users
      .AsNoTracking()
      .Include(x => x.Tags!)
      .ThenInclude(x => x.Devices)
      .FirstOrDefaultAsync(x => x.Id == userId);

    if (user?.Tags is null)
    {
      return;
    }

    var groupNames = user.Tags.Select(x => HubGroupNames.GetTagGroupName(x.Id, x.TenantId));
    await _agentHub.Clients.Groups(groupNames).ReceiveDto(wrapper);
  }

  public async Task SendPowerStateChange(Guid deviceId, PowerStateChangeType changeType)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return;
      }

      await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .ReceivePowerStateChange(changeType);

      var action = changeType == PowerStateChangeType.Restart ? AuditActions.Restart : AuditActions.Shutdown;
      AuditHubAction(AuditEventTypes.PowerState, action, authResult.Value);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending power state change.");
    }
  }

  public async Task<Result> RequestSafeModeReboot(Guid deviceId, bool withNetworking = true)
  {
    try
    {
      if (!IsActionVerified())
      {
        return Result.Fail("Action verification required. Please verify your identity first.");
      }

      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      var device = authResult.Value;

      if (device.Platform != SystemPlatform.Windows)
      {
        return Result.Fail("Safe Mode reboot is only supported on Windows devices.");
      }

      var request = new SafeModeRebootRequestHubDto(withNetworking);
      var result = await _agentHub.Clients
        .Client(device.ConnectionId)
        .RebootToSafeMode(request);

      if (result.IsSuccess)
      {
        AuditHubAction(AuditEventTypes.PowerState, AuditActions.SafeModeReboot, device,
          details: $"WithNetworking: {withNetworking}");
      }

      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while requesting Safe Mode reboot for device {DeviceId}.", deviceId);
      return Result.Fail("Agent could not be reached.");
    }
  }

  public async Task<Result> ResizePty(Guid deviceId, PtyResizeDto dto)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      var dtoWithViewer = dto with { ViewerConnectionId = Context.ConnectionId };

      return await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .ResizePty(dtoWithViewer);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while resizing PTY.");
      return Result.Fail("Agent could not be reached.");
    }
  }

  public async Task<Result> SendPtyInput(Guid deviceId, PtyInputDto dto)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      var dtoWithViewer = dto with { ViewerConnectionId = Context.ConnectionId };

      return await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .ReceivePtyInput(dtoWithViewer);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending PTY input.");
      return Result.Fail("Agent could not be reached.");
    }
  }

  public async Task<Result> SendTerminalInput(Guid deviceId, TerminalInputDto dto)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      // Create a new DTO with ViewerConnectionId
      var dtoWithViewerConnection = dto with { ViewerConnectionId = Context.ConnectionId };

      return await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .ReceiveTerminalInput(dtoWithViewerConnection);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending terminal input.");
      return Result.Fail("Agent could not be reached.");
    }
  }

  public async Task SendWakeDevice(Guid deviceId, string[] macAddresses)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return;
      }

      var tagsIds = await _appDb.Devices
        .Include(x => x.Tags)
        .Where(x => x.Id == deviceId)
        .SelectMany(x => x.Tags!)
        .Select(x => x.Id)
        .ToListAsync();

      var tagGroupNames = tagsIds.Select(tagId =>
        HubGroupNames.GetTagGroupName(tagId, authResult.Value.TenantId));

      var dto = new WakeDeviceDto(macAddresses);
      await _agentHub.Clients
        .Groups(tagGroupNames)
        .InvokeWakeDevice(dto);

      AuditHubAction(AuditEventTypes.WakeDevice, AuditActions.Invoke, authResult.Value,
        details: $"MACs: {string.Join(", ", macAddresses)}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while sending wake device command.");
    }
  }

  public async Task<Result> TestVncConnection(Guid guid, int port)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(guid) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      return await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .TestVncConnection(port);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while testing VNC connection.");
      return Result.Fail("An error occurred while testing the VNC connection.");
    }
  }

  public async Task<Result> RequestPatchScan(Guid deviceId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      var device = authResult.Value;

      if (device.Platform != SystemPlatform.Windows)
      {
        return Result.Fail("Patch scanning is only supported on Windows devices.");
      }

      var request = new PatchScanRequestHubDto();
      var result = await _agentHub.Clients
        .Client(device.ConnectionId)
        .ScanForPatches(request);

      if (result.IsSuccess)
      {
        AuditHubAction(AuditEventTypes.PatchManagement, AuditActions.Scan, device);
      }

      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while requesting patch scan for device {DeviceId}.", deviceId);
      return Result.Fail("Agent could not be reached.");
    }
  }

  public async Task<Result> RequestPatchInstall(Guid deviceId, string[] updateIds)
  {
    try
    {
      if (!IsActionVerified())
      {
        return Result.Fail("Action verification required. Please verify your identity first.");
      }

      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      if (!TryGetUserId(out var userId))
      {
        return Result.Fail("Failed to get user ID.");
      }

      if (!TryGetTenantId(out var tenantId))
      {
        return Result.Fail("Failed to get tenant ID.");
      }

      var device = authResult.Value;

      if (device.Platform != SystemPlatform.Windows)
      {
        return Result.Fail("Patch installation is only supported on Windows devices.");
      }

      // Create installation record
      var installation = new PatchInstallation
      {
        DeviceId = deviceId,
        InitiatedByUserId = userId,
        InitiatedAt = DateTimeOffset.UtcNow,
        TotalCount = updateIds.Length,
        Status = "InProgress",
        TenantId = tenantId,
      };
      await _appDb.PatchInstallations.AddAsync(installation);
      await _appDb.SaveChangesAsync();

      var request = new PatchInstallRequestHubDto(updateIds);
      var result = await _agentHub.Clients
        .Client(device.ConnectionId)
        .InstallPatches(request);

      if (result.IsSuccess)
      {
        AuditHubAction(AuditEventTypes.PatchManagement, AuditActions.Install, device,
          details: $"UpdateIds: {string.Join(", ", updateIds)}");
      }

      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while requesting patch install for device {DeviceId}.", deviceId);
      return Result.Fail("Agent could not be reached.");
    }
  }

  public async Task<Result<CreateJitAdminResponseDto>> RequestCreateJitAdmin(Guid deviceId, int ttlMinutes = 60)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail<CreateJitAdminResponseDto>("Unauthorized.");
      }

      var device = authResult.Value;

      if (device.Platform != SystemPlatform.Windows)
      {
        return Result.Fail<CreateJitAdminResponseDto>("JIT Admin accounts are only supported on Windows devices.");
      }

      if (string.IsNullOrWhiteSpace(device.ConnectionId))
      {
        return Result.Fail<CreateJitAdminResponseDto>("Device is not currently connected.");
      }

      if (!TryGetUserId(out var userId))
      {
        return Result.Fail<CreateJitAdminResponseDto>("Failed to get user ID.");
      }

      if (!TryGetTenantId(out var tenantId))
      {
        return Result.Fail<CreateJitAdminResponseDto>("Failed to get tenant ID.");
      }

      // Generate random username and password
      var hexSuffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant();
      var username = $"jit-admin-{hexSuffix}";
      var password = GenerateSecurePassword(16);

      // Clamp TTL between 5 and 1440 minutes (24 hours)
      ttlMinutes = Math.Clamp(ttlMinutes, 5, 1440);

      // Create DB entity
      var jitAccount = new JitAdminAccount
      {
        DeviceId = device.Id,
        DeviceName = device.Name,
        Username = username,
        CreatedByUserId = userId,
        CreatedByUserName = Context.User?.Identity?.Name,
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes),
        Status = JitAdminAccountStatus.Active,
        TenantId = tenantId,
      };

      await _appDb.JitAdminAccounts.AddAsync(jitAccount);
      await _appDb.SaveChangesAsync();

      // Send to agent
      var hubDto = new CreateJitAdminRequestHubDto(username, password, ttlMinutes);
      var agentResult = await _agentHub.Clients
        .Client(device.ConnectionId)
        .CreateJitAdminAccount(hubDto);

      if (!agentResult.IsSuccess)
      {
        jitAccount.Status = JitAdminAccountStatus.Failed;
        await _appDb.SaveChangesAsync();

        _logger.LogError(
          "Agent failed to create JIT admin account on device {DeviceId}: {Reason}",
          deviceId,
          agentResult.Reason);

        return Result.Fail<CreateJitAdminResponseDto>(
          $"Agent failed to create account: {agentResult.Reason}");
      }

      AuditHubAction(AuditEventTypes.JitAdmin, AuditActions.Create, device,
        details: $"Username: {username}, TTL: {ttlMinutes}min, Expires: {jitAccount.ExpiresAt:O}");

      var dto = jitAccount.ToDto();
      return Result.Ok(new CreateJitAdminResponseDto(dto, password));
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while creating JIT admin account for device {DeviceId}.", deviceId);
      return Result.Fail<CreateJitAdminResponseDto>("An error occurred while creating JIT admin account.");
    }
  }

  public async Task<Result> RequestDeleteJitAdmin(Guid deviceId, Guid jitAccountId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      var device = authResult.Value;

      if (!TryGetTenantId(out var tenantId))
      {
        return Result.Fail("Failed to get tenant ID.");
      }

      var jitAccount = await _appDb.JitAdminAccounts
        .FirstOrDefaultAsync(x => x.Id == jitAccountId && x.DeviceId == deviceId);

      if (jitAccount is null)
      {
        return Result.Fail("JIT admin account not found.");
      }

      if (jitAccount.Status != JitAdminAccountStatus.Active)
      {
        return Result.Fail("JIT admin account is not active.");
      }

      // Send delete to agent if device is online
      if (!string.IsNullOrWhiteSpace(device.ConnectionId))
      {
        try
        {
          var hubDto = new DeleteJitAdminRequestHubDto(jitAccount.Username);
          await _agentHub.Clients
            .Client(device.ConnectionId)
            .DeleteJitAdminAccount(hubDto);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex,
            "Failed to send delete command to agent for JIT account {Username} on device {DeviceId}. Marking as deleted anyway.",
            jitAccount.Username, deviceId);
        }
      }

      jitAccount.Status = JitAdminAccountStatus.ManuallyDeleted;
      jitAccount.DeletedAt = DateTimeOffset.UtcNow;
      await _appDb.SaveChangesAsync();

      AuditHubAction(AuditEventTypes.JitAdmin, AuditActions.Delete, device,
        details: $"Username: {jitAccount.Username}, ManualDelete");

      return Result.Ok();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while deleting JIT admin account {JitAccountId} on device {DeviceId}.", jitAccountId, deviceId);
      return Result.Fail("An error occurred while deleting JIT admin account.");
    }
  }

  public async Task UninstallAgent(Guid deviceId, string reason)
  {
    try
    {
      if (!IsActionVerified())
      {
        _logger.LogWarning("Action verification required for UninstallAgent. User: {UserName}.", Context.UserIdentifier);
        return;
      }

      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return;
      }

      _logger.LogInformation(
        "Agent uninstall command sent by user: {UserName}.  Device: {DeviceId}",
        Context.UserIdentifier,
        deviceId);

      await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .UninstallAgent(reason);

      AuditHubAction(AuditEventTypes.UninstallAgent, AuditActions.Invoke, authResult.Value,
        details: $"Reason: {reason}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error while uninstalling agent.");
    }
  }

  public async Task<Result> UploadFile(
    FileUploadMetadata fileUploadMetadata,
    ChannelReader<byte[]> fileStream)
  {
    try
    {
      var deviceId = fileUploadMetadata.DeviceId;

      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      var maxUploadSize = _appOptions.CurrentValue.MaxFileTransferSize;
      if (maxUploadSize > 0 && fileUploadMetadata.FileSize > maxUploadSize)
      {
        return Result.Fail($"File size exceeds the maximum allowed size of {maxUploadSize} bytes.");
      }

      var device = authResult.Value;
      if (string.IsNullOrWhiteSpace(device.ConnectionId))
      {
        _logger.LogWarning("Device {DeviceId} is not connected (no ConnectionId).", deviceId);
        return Result.Fail("Device is not currently connected.");
      }

      var streamId = Guid.NewGuid();
      using var signaler = _hubStreamStore.GetOrCreate<byte[]>(streamId, TimeSpan.FromMinutes(30));

      var uploadRequest = new FileUploadHubDto(
        streamId,
        fileUploadMetadata.TargetDirectory,
        fileUploadMetadata.FileName,
        fileUploadMetadata.FileSize,
        fileUploadMetadata.Overwrite);

      // Asynchronously write the client's stream to the channel.
      var writeTask = signaler.WriteFromChannelReader(fileStream, Context.ConnectionAborted);

      // Notify the agent about the incoming upload
      var receiveResult = await _agentHub.Clients
        .Client(device.ConnectionId)
        .DownloadFileFromViewer(uploadRequest)
        .WaitAsync(Context.ConnectionAborted);

      if (receiveResult is null || !receiveResult.IsSuccess)
      {
        var reason = receiveResult?.Reason ?? "Agent did not respond.";
        _logger.LogWarning("Device {DeviceId} failed to download file {FileName}.  Reason: {Reason}",
          deviceId,
          fileUploadMetadata.FileName,
          reason);
        return Result.Fail($"Agent failed to download file: {reason}");
      }

      // Await the write task to ensure all data is sent or an error occurs.
      try
      {
        await writeTask;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error writing file stream for {FileName} to device {DeviceId}",
          fileUploadMetadata.FileName, fileUploadMetadata.DeviceId);
        return Result.Fail("An error occurred while writing the file stream.");
      }

      AuditHubAction(AuditEventTypes.FileTransfer, AuditActions.Upload, device,
        details: $"File: {fileUploadMetadata.FileName}, Size: {fileUploadMetadata.FileSize}");

      return Result.Ok();
    }
    catch (OperationCanceledException)
    {
      _logger.LogInformation("File upload was canceled by the user for file {FileName} to device {DeviceId}",
        fileUploadMetadata.FileName,
        fileUploadMetadata.DeviceId);
      return Result.Fail("File upload was canceled.");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error uploading file {FileName} to device {DeviceId}",
        fileUploadMetadata.FileName, fileUploadMetadata.DeviceId);
      return Result.Fail("An error occurred during file upload.");
    }
  }

  private static Task<string> GetDisplayName(AppUser user, string fallbackName = "Admin")
  {
    var displayName = user.UserPreferences
      ?.FirstOrDefault(x => x.Name == UserPreferenceNames.UserDisplayName)
      ?.Value;

    if (string.IsNullOrWhiteSpace(displayName))
    {
      displayName = user.UserName ?? fallbackName;
    }

    return displayName.AsTaskResult();
  }

  public async Task<Result> StartStandaloneWebcam(Guid deviceId, int cameraIndex, int preferredWidth, int preferredHeight)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      if (string.IsNullOrWhiteSpace(authResult.Value.ConnectionId))
      {
        return Result.Fail("Device is not currently connected.");
      }

      return await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .StartStandaloneWebcam(Context.ConnectionId, cameraIndex, preferredWidth, preferredHeight);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error starting standalone webcam for device {DeviceId}.", deviceId);
      return Result.Fail("Agent could not be reached.");
    }
  }

  public async Task<Result> StopStandaloneWebcam(Guid deviceId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      if (string.IsNullOrWhiteSpace(authResult.Value.ConnectionId))
      {
        return Result.Fail("Device is not currently connected.");
      }

      return await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .StopStandaloneWebcam(Context.ConnectionId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error stopping standalone webcam for device {DeviceId}.", deviceId);
      return Result.Fail("Agent could not be reached.");
    }
  }

  public async Task<Result> GetStandaloneWebcamList(Guid deviceId)
  {
    try
    {
      if (await TryAuthorizeAgainstDevice(deviceId) is not { IsSuccess: true } authResult)
      {
        return Result.Fail("Unauthorized.");
      }

      if (string.IsNullOrWhiteSpace(authResult.Value.ConnectionId))
      {
        return Result.Fail("Device is not currently connected.");
      }

      return await _agentHub.Clients
        .Client(authResult.Value.ConnectionId)
        .GetWebcamList(Context.ConnectionId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting webcam list for device {DeviceId}.", deviceId);
      return Result.Fail("Agent could not be reached.");
    }
  }

  private void AuditHubAction(
    string eventType,
    string action,
    Device device,
    Guid? sessionId = null,
    string? details = null)
  {
    if (!TryGetTenantId(out var tenantId))
    {
      return;
    }

    TryGetUserId(out var userId);
    var sourceIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();

    _auditService.LogEvent(
      tenantId,
      eventType,
      action,
      actorUserId: userId,
      actorUserName: Context.User?.Identity?.Name,
      targetDeviceId: device.Id,
      targetDeviceName: device.Name,
      sourceIpAddress: sourceIp,
      sessionId: sessionId,
      details: details);
  }

  private async Task<Result<string>> GetDisplayName(Guid userId)
  {
    var user = await _userManager.Users
      .AsNoTracking()
      .Include(x => x.UserPreferences)
      .FirstOrDefaultAsync(x => x.Id == userId);

    if (user is null)
    {
      return Result
        .Fail<string>("User not found.")
        .Log(_logger);
    }

    var displayName = user.UserPreferences
      ?.FirstOrDefault(x => x.Name == UserPreferenceNames.UserDisplayName)
      ?.Value;

    if (string.IsNullOrWhiteSpace(displayName))
    {
      displayName = user.UserName ?? "";
    }
    return Result.Ok(displayName);
  }

  private async Task<AppUser> GetRequiredUser(Func<IQueryable<AppUser>, IQueryable<AppUser>>? includeBuilder = null)
  {
    if (!TryGetUserId(out var userId))
    {
      throw new UnauthorizedAccessException("Failed to get user ID.");
    }

    var query = _userManager.Users.AsNoTracking();

    if (includeBuilder is not null)
    {
      query = includeBuilder.Invoke(query);
    }

    var user = await query.FirstOrDefaultAsync(x => x.Id == userId);

    Guard.IsNotNull(user);
    return user;
  }

  private bool IsActionVerified()
  {
    if (!TryGetUserId(out var userId))
    {
      return false;
    }
    return _actionVerificationService.IsVerified(userId);
  }

  private bool IsServerAdmin()
  {
    return Context.User?.IsInRole(RoleNames.ServerAdministrator) ?? false;
  }

  private async Task<Result<Device>> TryAuthorizeAgainstDevice(
    Guid deviceId,
    [CallerMemberName] string? callerName = null)
  {
    if (Context.User is null)
    {
      _logger.LogCritical("User is null.  Authorize tag should have prevented this.");
      return Result.Fail<Device>("User is null.  Authorize tag should have prevented this.");
    }

    var isServerAdmin = Context.User.IsInRole(RoleNames.ServerAdministrator);
    var devicesQuery = isServerAdmin
      ? _appDb.Devices.IgnoreQueryFilters()
      : _appDb.Devices;

    var device = await devicesQuery
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == deviceId);

    if (device is null)
    {
      _logger.LogWarning("Device {DeviceId} not found.", deviceId);
      return Result.Fail<Device>("Device not found.");
    }

    var authResult = await _authorizationService.AuthorizeAsync(
      Context.User,
      device,
      DeviceAccessByDeviceResourcePolicy.PolicyName);

    if (authResult.Succeeded)
    {
      return Result.Ok(device);
    }

    _logger.LogCritical(
      "Unauthorized agent access attempted by user: {UserName}.  Device: {DeviceId}.  Method: {MemberName}.",
      Context.UserIdentifier,
      deviceId,
      callerName);

    return Result.Fail<Device>("Unauthorized.");
  }

  private bool TryGetTenantId(
    out Guid tenantId,
    [CallerMemberName] string callerName = "")
  {
    tenantId = Guid.Empty;
    if (Context.User?.TryGetTenantId(out tenantId) == true)
    {
      return true;
    }

    _logger.LogError("TenantId claim is unexpected missing when calling {MemberName}.", callerName);
    return false;
  }

  private bool TryGetUserId(
    out Guid userId,
    [CallerMemberName] string callerName = "")
  {
    userId = Guid.Empty;
    if (Context.User?.TryGetUserId(out userId) == true)
    {
      return true;
    }

    _logger.LogError("UserId claim is unexpected missing when calling {MemberName}.", callerName);
    return false;
  }

  private static string GenerateSecurePassword(int length)
  {
    const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string lower = "abcdefghijklmnopqrstuvwxyz";
    const string digits = "0123456789";
    const string special = "!@#$%&*?";
    const string allChars = upper + lower + digits + special;

    Span<char> password = stackalloc char[length];

    // Ensure at least one of each category
    password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
    password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
    password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
    password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

    for (var i = 4; i < length; i++)
    {
      password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
    }

    // Shuffle using Fisher-Yates
    for (var i = length - 1; i > 0; i--)
    {
      var j = RandomNumberGenerator.GetInt32(i + 1);
      (password[i], password[j]) = (password[j], password[i]);
    }

    return new string(password);
  }
}