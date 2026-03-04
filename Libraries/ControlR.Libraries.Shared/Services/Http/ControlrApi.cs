using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ControlR.Libraries.Shared.Constants;
using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using ControlR.Libraries.Shared.Enums;

namespace ControlR.Libraries.Shared.Services.Http;

public interface IControlrApi
{
  Task<Result<AcceptInvitationResponseDto>> AcceptInvitation(
      string activationCode,
      string emailAddress,
      string password);
  Task<Result<AlertDto>> AcknowledgeAlert(Guid alertId);
  Task<Result> AddDeviceTag(Guid deviceId, Guid tagId);
  Task<Result> AddUserRole(Guid userId, Guid roleId);
  Task<Result> AddUserTag(Guid userId, Guid tagId);
  Task<Result<AlertRuleDto>> CreateAlertRule(AlertRuleCreateRequestDto request);
  Task<Result<ClientDeviceAssignmentDto>> CreateClientDeviceAssignment(ClientDeviceAssignmentCreateRequestDto request);
  Task<Result> CreateDevice(DeviceUpdateRequestDto device, Guid installerKeyId, string installerKeySecret, Guid[]? tagIds);
  Task<Result<DeviceGroupDto>> CreateDeviceGroup(DeviceGroupCreateRequestDto request);
  Task<Result> CreateDirectory(Guid deviceId, string parentPath, string directoryName);
  Task<Result<CreateInstallerKeyResponseDto>> CreateInstallerKey(CreateInstallerKeyRequestDto dto);
  Task<Result<CreatePersonalAccessTokenResponseDto>> CreatePersonalAccessToken(CreatePersonalAccessTokenRequestDto request);
  Task<Result<ScheduledTaskDto>> CreateScheduledTask(ScheduledTaskCreateRequestDto request);
  Task<Result<SavedScriptDto>> CreateScript(SavedScriptCreateRequestDto request);
  Task<Result<TagResponseDto>> CreateTag(string tagName, TagType tagType);
  Task<Result<TenantInviteResponseDto>> CreateTenantInvite(string inviteeEmail);
  Task<Result<WebhookSubscriptionDto>> CreateWebhook(WebhookCreateRequestDto request);
  Task<Result> DeleteAlertRule(Guid ruleId);
  Task<Result> DeleteClientDeviceAssignment(Guid assignmentId);
  Task<Result> DeleteDevice(Guid deviceId);
  Task<Result> DeleteDeviceGroup(Guid groupId);
  Task<Result> DeleteFile(Guid deviceId, string filePath, bool isDirectory);
  Task<Result> DeleteInstallerKey(Guid keyId);
  Task<Result> DeletePersonalAccessToken(Guid personalAccessTokenId);
  Task<Result> DeleteScheduledTask(Guid taskId);
  Task<Result> DeleteScript(Guid scriptId);
  Task<Result> DeleteTag(Guid tagId);
  Task<Result> DeleteTenantInvite(Guid inviteId);
  Task<Result> DeleteTenantSetting(string settingName);
  Task<Result> DeleteUser(Guid userId);
  Task<Result> DeleteWebhook(Guid webhookId);
  Task<Result<Stream>> DownloadFile(Guid deviceId, string filePath);
  Task<Result<AlertDto[]>> GetAlerts(string? status = null, int count = 50);
  Task<Result<AlertRuleDto[]>> GetAllAlertRules();
  Task<Result<DeviceGroupDto[]>> GetAllDeviceGroups();
  IAsyncEnumerable<DeviceResponseDto> GetAllDevices();
  Task<Result<AgentInstallerKeyDto[]>> GetAllInstallerKeys();
  Task<Result<RoleResponseDto[]>> GetAllRoles();
  Task<Result<ScheduledTaskDto[]>> GetAllScheduledTasks();
  Task<Result<SavedScriptDto[]>> GetAllScripts();
  Task<Result<TagResponseDto[]>> GetAllTags(bool includeLinkedIds = false);
  Task<Result<UserResponseDto[]>> GetAllUsers();
  Task<Result<WebhookSubscriptionDto[]>> GetAllWebhooks();
  Task<Result<TagResponseDto[]>> GetAllowedTags();
  Task<Result<GetAspireUrlResponseDto>> GetAspireUrl();
  Task<Result<AuditLogDto>> GetAuditLog(Guid id);
  Task<Result<ClientDeviceAssignmentDto[]>> GetClientDeviceAssignments(Guid userId);
  Task<Result<DeviceResponseDto[]>> GetClientDevices();
  Task<Result<string>> GetCurrentAgentHashSha256(RuntimeId runtime, CancellationToken cancellationToken = default);
  Task<Result<Version>> GetCurrentAgentVersion();
  Task<Result<Version>> GetCurrentServerVersion();
  Task<Result<byte[]>> GetDesktopPreview(Guid deviceId, int targetProcessId);
  Task<Result<DeviceResponseDto>> GetDevice(Guid deviceId);
  Task<Result<DeviceHardwareDto>> GetDeviceHardware(Guid deviceId);
  Task<Result<DeviceMetricSnapshotDto[]>> GetDeviceMetrics(Guid deviceId, int hours = 24);
  Task<Result<SoftwareInventoryItemDto[]>> GetDeviceSoftware(Guid deviceId);
  Task<Result<InstalledUpdateDto[]>> GetDeviceUpdates(Guid deviceId);
  Task<Result<GetDirectoryContentsResponseDto>> GetDirectoryContents(Guid deviceId, string directoryPath);
  Task<Result<long>> GetFileUploadMaxSize();
  Task<Result<AgentInstallerKeyUsageDto[]>> GetInstallerKeyUsages(Guid keyId);
  Task<Result<string>> GetLogFileContents(Guid deviceId, string filePath);
  Task<Result<GetLogFilesResponseDto>> GetLogFiles(Guid deviceId);
  Task<Result<PathSegmentsResponseDto>> GetPathSegments(Guid deviceId, string targetPath);
  Task<Result<TenantInviteResponseDto[]>> GetPendingTenantInvites();
  Task<Result<PersonalAccessTokenDto[]>> GetPersonalAccessTokens();
  Task<Result<PublicRegistrationSettings>> GetPublicRegistrationSettings();
  Task<Result<ScriptExecutionDto[]>> GetRecentScriptExecutions(int count = 50);
  Task<Result<GetRootDrivesResponseDto>> GetRootDrives(Guid deviceId);
  Task<Result<ScheduledTaskDto>> GetScheduledTask(Guid taskId);
  Task<Result<ScheduledTaskExecutionDto[]>> GetScheduledTaskExecutions(Guid taskId, int count = 20);
  Task<Result<ScriptExecutionDto>> GetScriptExecution(Guid executionId);
  Task<Result<ServerAlertResponseDto?>> GetServerAlert();
  Task<Result<ServerStatsDto>> GetServerStats();
  Task<Result<GetSubdirectoriesResponseDto>> GetSubdirectories(Guid deviceId, string directoryPath);
  Task<Result<TenantSettingResponseDto?>> GetTenantSetting(string settingName);
  Task<Result<UserPreferenceResponseDto?>> GetUserPreference(string preferenceName);
  Task<Result<TagResponseDto[]>> GetUserTags(Guid userId, bool includeLinkedIds = false);
  Task<Result<WebhookDeliveryLogDto[]>> GetWebhookDeliveries(Guid webhookId, int count = 50);
  Task<Result> LogOut();
  Task<Result> RemoveDeviceTag(Guid deviceId, Guid tagId);
  Task<Result> RemoveUserRole(Guid userId, Guid roleId);
  Task<Result> RemoveUserTag(Guid userId, Guid tagId);
  Task<Result> RenameInstallerKey(RenameInstallerKeyRequestDto dto);
  Task<Result<TagResponseDto>> RenameTag(Guid tagId, string newTagName);
  Task<Result<AlertDto>> ResolveAlert(Guid alertId);
  Task<Result<AuditLogSearchResponseDto>> SearchAuditLogs(AuditLogSearchRequestDto request);
  Task<Result<DeviceSearchResponseDto>> SearchDevices(DeviceSearchRequestDto request);
  Task<Result<InventorySearchResultDto[]>> SearchInventory(InventorySearchRequestDto request);
  Task<Result> SendTestEmail();
  Task<Result<TenantSettingResponseDto>> SetTenantSetting(string settingName, string settingValue);
  Task<Result<UserPreferenceResponseDto>> SetUserPreference(string preferenceName, string preferenceValue);
  Task<Result> TestWebhook(Guid webhookId);
  Task<Result<ScheduledTaskExecutionDto>> TriggerScheduledTask(Guid taskId);
  Task<Result<AlertRuleDto>> UpdateAlertRule(AlertRuleUpdateRequestDto request);
  Task<Result<DeviceGroupDto>> UpdateDeviceGroup(DeviceGroupUpdateRequestDto request);
  Task<Result<PersonalAccessTokenDto>> UpdatePersonalAccessToken(Guid personalAccessTokenId, UpdatePersonalAccessTokenRequestDto request);
  Task<Result<ScheduledTaskDto>> UpdateScheduledTask(ScheduledTaskUpdateRequestDto request);
  Task<Result<SavedScriptDto>> UpdateScript(SavedScriptUpdateRequestDto request);
  Task<Result<ServerAlertResponseDto>> UpdateServerAlert(ServerAlertRequestDto request);
  Task<Result<WebhookSubscriptionDto>> UpdateWebhook(WebhookUpdateRequestDto request);
  Task<Result<ValidateFilePathResponseDto>> ValidateFilePath(Guid deviceId, string directoryPath, string fileName);
}

public class ControlrApi(
  HttpClient httpClient,
  ILogger<ControlrApi> logger) : IControlrApi
{
  private readonly HttpClient _client = httpClient;
  private readonly ILogger<ControlrApi> _logger = logger;

  public async Task<Result<AcceptInvitationResponseDto>> AcceptInvitation(
    string activationCode,
    string emailAddress,
    string password)
  {
    return await TryCallApi(async () =>
    {
      var dto = new AcceptInvitationRequestDto(activationCode, emailAddress, password);
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.InvitesEndpoint}/accept", dto);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<AcceptInvitationResponseDto>();
    });
  }

  public async Task<Result<AlertDto>> AcknowledgeAlert(Guid alertId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsync($"{HttpConstants.AlertsEndpoint}/{alertId}/acknowledge", null);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<AlertDto>();
    });
  }

  public async Task<Result> AddDeviceTag(Guid deviceId, Guid tagId)
  {
    return await TryCallApi(async () =>
    {
      var dto = new DeviceTagAddRequestDto(deviceId, tagId);
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.DeviceTagsEndpoint}", dto);
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> AddUserRole(Guid userId, Guid roleId)
  {
    return await TryCallApi(async () =>
    {
      var dto = new UserRoleAddRequestDto(userId, roleId);
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.UserRolesEndpoint}", dto);
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> AddUserTag(Guid userId, Guid tagId)
  {
    return await TryCallApi(async () =>
    {
      var dto = new UserTagAddRequestDto(userId, tagId);
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.UserTagsEndpoint}", dto);
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result<AlertRuleDto>> CreateAlertRule(AlertRuleCreateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync(HttpConstants.AlertRulesEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<AlertRuleDto>();
    });
  }

  public async Task<Result<ClientDeviceAssignmentDto>> CreateClientDeviceAssignment(ClientDeviceAssignmentCreateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.ClientPortalEndpoint}/assignments", request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<ClientDeviceAssignmentDto>();
    });
  }

  public async Task<Result> CreateDevice(DeviceUpdateRequestDto device, Guid installerKeyId, string installerKeySecret, Guid[]? tagIds)
  {
    return await TryCallApi(async () =>
    {
      var requestDto = new CreateDeviceRequestDto(device, installerKeyId, installerKeySecret, tagIds);
      using var response = await _client.PostAsJsonAsync(HttpConstants.DevicesEndpoint, requestDto);
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result<DeviceGroupDto>> CreateDeviceGroup(DeviceGroupCreateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync(HttpConstants.DeviceGroupsEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<DeviceGroupDto>();
    });
  }

  public async Task<Result> CreateDirectory(Guid deviceId, string parentPath, string directoryName)
  {
    return await TryCallApi(async () =>
    {
      var requestDto = new CreateDirectoryRequestDto(deviceId, parentPath, directoryName);
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.DeviceFileSystemEndpoint}/create-directory/{deviceId}", requestDto);
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result<CreateInstallerKeyResponseDto>> CreateInstallerKey(CreateInstallerKeyRequestDto dto)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync(HttpConstants.InstallerKeysEndpoint, dto);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<CreateInstallerKeyResponseDto>();
    });
  }

  public async Task<Result<CreatePersonalAccessTokenResponseDto>> CreatePersonalAccessToken(CreatePersonalAccessTokenRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync(HttpConstants.PersonalAccessTokensEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<CreatePersonalAccessTokenResponseDto>();
    });
  }

  public async Task<Result<ScheduledTaskDto>> CreateScheduledTask(ScheduledTaskCreateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync(HttpConstants.ScheduledTasksEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<ScheduledTaskDto>();
    });
  }

  public async Task<Result<SavedScriptDto>> CreateScript(SavedScriptCreateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync(HttpConstants.ScriptsEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<SavedScriptDto>();
    });
  }

  public async Task<Result<TagResponseDto>> CreateTag(string tagName, TagType tagType)
  {
    return await TryCallApi(async () =>
    {
      var request = new TagCreateRequestDto(tagName, tagType);
      using var response = await _client.PostAsJsonAsync(HttpConstants.TagsEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<TagResponseDto>();
    });
  }

  public async Task<Result<TenantInviteResponseDto>> CreateTenantInvite(string inviteeEmail)
  {
    return await TryCallApi(async () =>
    {
      var request = new TenantInviteRequestDto(inviteeEmail);
      using var response = await _client.PostAsJsonAsync(HttpConstants.InvitesEndpoint, request);
      if (response.StatusCode == HttpStatusCode.Conflict)
      {
        var content = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(content))
        {
          return Result.Fail<TenantInviteResponseDto>(content);
        }
      }
      response.EnsureSuccessStatusCode();
      var inviteDto = await response.Content.ReadFromJsonAsync<TenantInviteResponseDto>();
      if (inviteDto is null)
      {
        return Result.Fail<TenantInviteResponseDto>("Server response was empty.");
      }

      return Result.Ok(inviteDto);
    });
  }

  public async Task<Result<WebhookSubscriptionDto>> CreateWebhook(WebhookCreateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync(HttpConstants.WebhooksEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<WebhookSubscriptionDto>();
    });
  }

  public async Task<Result> DeleteAlertRule(Guid ruleId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.AlertRulesEndpoint}/{ruleId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteClientDeviceAssignment(Guid assignmentId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.ClientPortalEndpoint}/assignments/{assignmentId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteDevice(Guid deviceId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.DevicesEndpoint}/{deviceId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteDeviceGroup(Guid groupId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.DeviceGroupsEndpoint}/{groupId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteFile(Guid deviceId, string filePath, bool isDirectory)
  {
    return await TryCallApi(async () =>
    {
      var dto = new { FilePath = filePath, IsDirectory = isDirectory };
      using var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{HttpConstants.DeviceFileSystemEndpoint}/delete/{deviceId}")
      {
        Content = JsonContent.Create(dto)
      });
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteInstallerKey(Guid keyId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.InstallerKeysEndpoint}/{keyId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeletePersonalAccessToken(Guid personalAccessTokenId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.PersonalAccessTokensEndpoint}/{personalAccessTokenId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteScheduledTask(Guid taskId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.ScheduledTasksEndpoint}/{taskId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteScript(Guid scriptId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.ScriptsEndpoint}/{scriptId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteTag(Guid tagId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.TagsEndpoint}/{tagId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteTenantInvite(Guid inviteId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.InvitesEndpoint}/{inviteId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteTenantSetting(string settingName)
  {
    return await TryCallApi(async () =>
    {
      var url = $"{HttpConstants.TenantSettingsEndpoint}/{settingName}";
      using var response = await _client.DeleteAsync(url);
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteUser(Guid userId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.UsersEndpoint}/{userId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> DeleteWebhook(Guid webhookId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.WebhooksEndpoint}/{webhookId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result<Stream>> DownloadFile(Guid deviceId, string filePath)
  {
    return await TryCallApi(async () =>
    {
      var response = await _client.GetAsync($"{HttpConstants.DeviceFileSystemEndpoint}/download/{deviceId}?filePath={Uri.EscapeDataString(filePath)}");
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadAsStreamAsync();
    });
  }

  public async Task<Result<AlertDto[]>> GetAlerts(string? status = null, int count = 50)
  {
    return await TryCallApi(async () =>
    {
      var url = $"{HttpConstants.AlertsEndpoint}?count={count}";
      if (!string.IsNullOrWhiteSpace(status))
      {
        url += $"&status={status}";
      }
      return await _client.GetFromJsonAsync<AlertDto[]>(url);
    });
  }

  public async Task<Result<AlertRuleDto[]>> GetAllAlertRules()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<AlertRuleDto[]>(HttpConstants.AlertRulesEndpoint));
  }

  public async Task<Result<DeviceGroupDto[]>> GetAllDeviceGroups()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<DeviceGroupDto[]>(HttpConstants.DeviceGroupsEndpoint));
  }

  public async IAsyncEnumerable<DeviceResponseDto> GetAllDevices()
  {
    var stream = _client.GetFromJsonAsAsyncEnumerable<DeviceResponseDto>(HttpConstants.DevicesEndpoint);
    await foreach (var device in stream)
    {
      if (device is null)
      {
        continue;
      }

      yield return device;
    }
  }

  public async Task<Result<AgentInstallerKeyDto[]>> GetAllInstallerKeys()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<AgentInstallerKeyDto[]>(HttpConstants.InstallerKeysEndpoint));
  }

  public async Task<Result<RoleResponseDto[]>> GetAllRoles()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<RoleResponseDto[]>(HttpConstants.RolesEndpoint));
  }

  public async Task<Result<ScheduledTaskDto[]>> GetAllScheduledTasks()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<ScheduledTaskDto[]>(HttpConstants.ScheduledTasksEndpoint));
  }

  public async Task<Result<SavedScriptDto[]>> GetAllScripts()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<SavedScriptDto[]>(HttpConstants.ScriptsEndpoint));
  }

  public async Task<Result<TagResponseDto[]>> GetAllTags(bool includeLinkedIds = false)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<TagResponseDto[]>(
        $"{HttpConstants.TagsEndpoint}?includeLinkedIds={includeLinkedIds}"));
  }

  public async Task<Result<UserResponseDto[]>> GetAllUsers()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<UserResponseDto[]>(HttpConstants.UsersEndpoint));
  }

  public async Task<Result<WebhookSubscriptionDto[]>> GetAllWebhooks()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<WebhookSubscriptionDto[]>(HttpConstants.WebhooksEndpoint));
  }

  public async Task<Result<TagResponseDto[]>> GetAllowedTags()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<TagResponseDto[]>(HttpConstants.UserTagsEndpoint));
  }

  public async Task<Result<GetAspireUrlResponseDto>> GetAspireUrl()
  {
    return await TryCallApi(
      send: async () => await _client.GetAsync($"{HttpConstants.ServerLogsEndpoint}/get-aspire-url"),
      readSuccess: async response => await response.Content.ReadFromJsonAsync<GetAspireUrlResponseDto>() 
        ?? throw new HttpRequestException("The server response was empty."));
  }

  public async Task<Result<AuditLogDto>> GetAuditLog(Guid id)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<AuditLogDto>($"{HttpConstants.AuditLogsEndpoint}/{id}"));
  }

  public async Task<Result<ClientDeviceAssignmentDto[]>> GetClientDeviceAssignments(Guid userId)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<ClientDeviceAssignmentDto[]>(
        $"{HttpConstants.ClientPortalEndpoint}/assignments/{userId}"));
  }

  public async Task<Result<DeviceResponseDto[]>> GetClientDevices()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<DeviceResponseDto[]>(
        $"{HttpConstants.ClientPortalEndpoint}/devices"));
  }

  public async Task<Result<string>> GetCurrentAgentHashSha256(RuntimeId runtime, CancellationToken cancellationToken = default)
  {
    return await TryCallApi(async () => await _client.GetStringAsync(
      $"{HttpConstants.AgentUpdateEndpoint}/get-hash-sha256/{runtime}",
      cancellationToken));
  }

  public async Task<Result<Version>> GetCurrentAgentVersion()
  {
    return await TryCallApi(async () =>
    {
      var version = await _client.GetFromJsonAsync<Version>(HttpConstants.AgentVersionEndpoint);
      _logger.LogInformation("Latest Agent version on server: {LatestAgentVersion}", version);
      return version;
    });
  }

  public async Task<Result<Version>> GetCurrentServerVersion()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<Version>(HttpConstants.ServerVersionEndpoint));
  }

  public async Task<Result<byte[]>> GetDesktopPreview(Guid deviceId, int targetProcessId)
  {
    return await TryCallApi(
      async () => await _client.GetAsync($"{HttpConstants.DesktopPreviewEndpoint}/{deviceId}/{targetProcessId}"),
      async response => await response.Content.ReadAsByteArrayAsync());
  }

  public async Task<Result<DeviceResponseDto>> GetDevice(Guid deviceId)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<DeviceResponseDto>($"{HttpConstants.DevicesEndpoint}/{deviceId}"));
  }

  public async Task<Result<DeviceHardwareDto>> GetDeviceHardware(Guid deviceId)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<DeviceHardwareDto>(
        $"{HttpConstants.InventoryEndpoint}/{deviceId}/hardware"));
  }

  public async Task<Result<DeviceMetricSnapshotDto[]>> GetDeviceMetrics(Guid deviceId, int hours = 24)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<DeviceMetricSnapshotDto[]>(
        $"{HttpConstants.MetricsEndpoint}/{deviceId}?hours={hours}"));
  }

  public async Task<Result<SoftwareInventoryItemDto[]>> GetDeviceSoftware(Guid deviceId)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<SoftwareInventoryItemDto[]>(
        $"{HttpConstants.InventoryEndpoint}/{deviceId}/software"));
  }

  public async Task<Result<InstalledUpdateDto[]>> GetDeviceUpdates(Guid deviceId)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<InstalledUpdateDto[]>(
        $"{HttpConstants.InventoryEndpoint}/{deviceId}/updates"));
  }

  public async Task<Result<GetDirectoryContentsResponseDto>> GetDirectoryContents(Guid deviceId, string directoryPath)
  {
    return await TryCallApi(async () =>
    {
      var dto = new GetDirectoryContentsRequestDto(deviceId, directoryPath);
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.DeviceFileSystemEndpoint}/contents", dto);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<GetDirectoryContentsResponseDto>();
    });
  }

  public async Task<Result<long>> GetFileUploadMaxSize()
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.GetAsync($"{HttpConstants.UserServerSettingsEndpoint}/file-upload-max-size");
      response.EnsureSuccessStatusCode();
      var dto = await response.Content.ReadFromJsonAsync<FileUploadMaxSizeResponseDto>()
        ?? throw new HttpRequestException("The server response was empty.");
      return dto.MaxFileSize;
    });
  }

  public async Task<Result<AgentInstallerKeyUsageDto[]>> GetInstallerKeyUsages(Guid keyId)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<AgentInstallerKeyUsageDto[]>($"{HttpConstants.InstallerKeysEndpoint}/usages/{keyId}"));
  }

  public async Task<Result<string>> GetLogFileContents(Guid deviceId, string filePath)
  {
    return await TryCallApi(async () =>
    {
      var encodedPath = Uri.EscapeDataString(filePath);
      using var response = await _client.GetAsync($"{HttpConstants.DeviceFileSystemEndpoint}/logs/{deviceId}/contents?filePath={encodedPath}");
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadAsStringAsync();
    });
  }

  public async Task<Result<GetLogFilesResponseDto>> GetLogFiles(Guid deviceId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.GetAsync($"{HttpConstants.DeviceFileSystemEndpoint}/logs/{deviceId}");
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<GetLogFilesResponseDto>();
    });
  }

  public async Task<Result<PathSegmentsResponseDto>> GetPathSegments(Guid deviceId, string targetPath)
  {
    return await TryCallApi(async () =>
    {
      var dto = new GetPathSegmentsRequestDto(deviceId, targetPath);
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.DeviceFileSystemEndpoint}/path-segments", dto);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<PathSegmentsResponseDto>();
    });
  }

  public async Task<Result<TenantInviteResponseDto[]>> GetPendingTenantInvites()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<TenantInviteResponseDto[]>(HttpConstants.InvitesEndpoint));
  }

  public async Task<Result<PersonalAccessTokenDto[]>> GetPersonalAccessTokens()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<PersonalAccessTokenDto[]>(HttpConstants.PersonalAccessTokensEndpoint));
  }

  public async Task<Result<PublicRegistrationSettings>> GetPublicRegistrationSettings()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<PublicRegistrationSettings>(HttpConstants.PublicRegistrationSettingsEndpoint));
  }

  public async Task<Result<ScriptExecutionDto[]>> GetRecentScriptExecutions(int count = 50)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<ScriptExecutionDto[]>(
        $"{HttpConstants.ScriptExecutionsEndpoint}?count={count}"));
  }

  public async Task<Result<GetRootDrivesResponseDto>> GetRootDrives(Guid deviceId)
  {
    return await TryCallApi(async () =>
    {
      var dto = new GetRootDrivesRequestDto(deviceId);
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.DeviceFileSystemEndpoint}/root-drives", dto);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<GetRootDrivesResponseDto>();
    });
  }

  public async Task<Result<ScheduledTaskDto>> GetScheduledTask(Guid taskId)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<ScheduledTaskDto>(
        $"{HttpConstants.ScheduledTasksEndpoint}/{taskId}"));
  }

  public async Task<Result<ScheduledTaskExecutionDto[]>> GetScheduledTaskExecutions(Guid taskId, int count = 20)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<ScheduledTaskExecutionDto[]>(
        $"{HttpConstants.ScheduledTasksEndpoint}/{taskId}/executions?count={count}"));
  }

  public async Task<Result<ScriptExecutionDto>> GetScriptExecution(Guid executionId)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<ScriptExecutionDto>(
        $"{HttpConstants.ScriptExecutionsEndpoint}/{executionId}"));
  }

  public async Task<Result<ServerAlertResponseDto?>> GetServerAlert()
  {
    return await TryGetNullableResponse(async () =>
    {
      using var response = await _client.GetAsync(HttpConstants.ServerAlertEndpoint);
      if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
      {
        return null;
      }
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<ServerAlertResponseDto>();
    });
  }

  public async Task<Result<ServerStatsDto>> GetServerStats()
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<ServerStatsDto>(HttpConstants.ServerStatsEndpoint));
  }

  public async Task<Result<GetSubdirectoriesResponseDto>> GetSubdirectories(Guid deviceId, string directoryPath)
  {
    return await TryCallApi(async () =>
    {
      var dto = new GetSubdirectoriesRequestDto(deviceId, directoryPath);
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.DeviceFileSystemEndpoint}/subdirectories", dto);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<GetSubdirectoriesResponseDto>();
    });
  }

  public async Task<Result<TenantSettingResponseDto?>> GetTenantSetting(string settingName)
  {
    return await TryGetNullableResponse(async () =>
    {
      using var response = await _client.GetAsync($"{HttpConstants.TenantSettingsEndpoint}/{settingName}");
      if (response.StatusCode == HttpStatusCode.NoContent)
      {
        return null;
      }
      return await response.Content.ReadFromJsonAsync<TenantSettingResponseDto>();
    });
  }

  public async Task<Result<UserPreferenceResponseDto?>> GetUserPreference(string preferenceName)
  {
    return await TryGetNullableResponse(async () =>
    {
      using var response = await _client.GetAsync($"{HttpConstants.UserPreferencesEndpoint}/{preferenceName}");
      if (response.StatusCode == HttpStatusCode.NoContent)
      {
        return null;
      }
      return await response.Content.ReadFromJsonAsync<UserPreferenceResponseDto>();
    });
  }

  public async Task<Result<TagResponseDto[]>> GetUserTags(Guid userId, bool includeLinkedIds = false)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<TagResponseDto[]>(
        $"{HttpConstants.UserTagsEndpoint}/{userId}"));
  }

  public async Task<Result<WebhookDeliveryLogDto[]>> GetWebhookDeliveries(Guid webhookId, int count = 50)
  {
    return await TryCallApi(async () =>
      await _client.GetFromJsonAsync<WebhookDeliveryLogDto[]>(
        $"{HttpConstants.WebhooksEndpoint}/{webhookId}/deliveries?count={count}"));
  }

  public async Task<Result> LogOut()
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync(HttpConstants.LogoutEndpoint, new { });
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> RemoveDeviceTag(Guid deviceId, Guid tagId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.DeviceTagsEndpoint}/{deviceId}/{tagId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> RemoveUserRole(Guid userId, Guid roleId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.UserRolesEndpoint}/{userId}/{roleId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> RemoveUserTag(Guid userId, Guid tagId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.DeleteAsync($"{HttpConstants.UserTagsEndpoint}/{userId}/{tagId}");
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result> RenameInstallerKey(RenameInstallerKeyRequestDto dto)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PutAsJsonAsync($"{HttpConstants.InstallerKeysEndpoint}/rename", dto);
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result<TagResponseDto>> RenameTag(Guid tagId, string newTagName)
  {
    return await TryCallApi(async () =>
    {
      var dto = new TagRenameRequestDto(tagId, newTagName);
      using var response = await _client.PutAsJsonAsync($"{HttpConstants.TagsEndpoint}", dto);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<TagResponseDto>();
    });
  }

  public async Task<Result<AlertDto>> ResolveAlert(Guid alertId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsync($"{HttpConstants.AlertsEndpoint}/{alertId}/resolve", null);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<AlertDto>();
    });
  }

  public async Task<Result<AuditLogSearchResponseDto>> SearchAuditLogs(AuditLogSearchRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.AuditLogsEndpoint}/search", request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<AuditLogSearchResponseDto>();
    });
  }

  public async Task<Result<DeviceSearchResponseDto>> SearchDevices(DeviceSearchRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.DevicesEndpoint}/search", request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<DeviceSearchResponseDto>();
    });
  }

  public async Task<Result<InventorySearchResultDto[]>> SearchInventory(InventorySearchRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.InventoryEndpoint}/search", request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<InventorySearchResultDto[]>();
    });
  }

  public async Task<Result> SendTestEmail()
  {
    return await TryCallApi(() => _client.PostAsync(HttpConstants.TestEmailEndpoint, null));
  }

  public async Task<Result<TenantSettingResponseDto>> SetTenantSetting(string settingName, string settingValue)
  {
    return await TryCallApi(async () =>
    {
      var request = new TenantSettingRequestDto(settingName, settingValue);
      using var response = await _client.PostAsJsonAsync(HttpConstants.TenantSettingsEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<TenantSettingResponseDto>();
    });
  }

  public async Task<Result<UserPreferenceResponseDto>> SetUserPreference(string preferenceName, string preferenceValue)
  {
    return await TryCallApi(async () =>
    {
      var request = new UserPreferenceRequestDto(preferenceName, preferenceValue);
      using var response = await _client.PostAsJsonAsync(HttpConstants.UserPreferencesEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<UserPreferenceResponseDto>();
    });
  }

  public async Task<Result> TestWebhook(Guid webhookId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsync($"{HttpConstants.WebhooksEndpoint}/{webhookId}/test", null);
      response.EnsureSuccessStatusCode();
    });
  }

  public async Task<Result<ScheduledTaskExecutionDto>> TriggerScheduledTask(Guid taskId)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsync($"{HttpConstants.ScheduledTasksEndpoint}/{taskId}/trigger", null);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<ScheduledTaskExecutionDto>();
    });
  }

  public async Task<Result<AlertRuleDto>> UpdateAlertRule(AlertRuleUpdateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PutAsJsonAsync(HttpConstants.AlertRulesEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<AlertRuleDto>();
    });
  }

  public async Task<Result<DeviceGroupDto>> UpdateDeviceGroup(DeviceGroupUpdateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PutAsJsonAsync(HttpConstants.DeviceGroupsEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<DeviceGroupDto>();
    });
  }

  public async Task<Result<PersonalAccessTokenDto>> UpdatePersonalAccessToken(Guid personalAccessTokenId, UpdatePersonalAccessTokenRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PutAsJsonAsync($"{HttpConstants.PersonalAccessTokensEndpoint}/{personalAccessTokenId}", request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<PersonalAccessTokenDto>();
    });
  }

  public async Task<Result<ScheduledTaskDto>> UpdateScheduledTask(ScheduledTaskUpdateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PutAsJsonAsync(HttpConstants.ScheduledTasksEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<ScheduledTaskDto>();
    });
  }

  public async Task<Result<SavedScriptDto>> UpdateScript(SavedScriptUpdateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PutAsJsonAsync(HttpConstants.ScriptsEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<SavedScriptDto>();
    });
  }

  public async Task<Result<ServerAlertResponseDto>> UpdateServerAlert(ServerAlertRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PostAsJsonAsync(HttpConstants.ServerAlertEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<ServerAlertResponseDto>();
    });
  }

  public async Task<Result<WebhookSubscriptionDto>> UpdateWebhook(WebhookUpdateRequestDto request)
  {
    return await TryCallApi(async () =>
    {
      using var response = await _client.PutAsJsonAsync(HttpConstants.WebhooksEndpoint, request);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<WebhookSubscriptionDto>();
    });
  }

  public async Task<Result<ValidateFilePathResponseDto>> ValidateFilePath(Guid deviceId, string directoryPath, string fileName)
  {
    return await TryCallApi(async () =>
    {
      var requestDto = new ValidateFilePathRequestDto(deviceId, directoryPath, fileName);
      using var response = await _client.PostAsJsonAsync($"{HttpConstants.DeviceFileSystemEndpoint}/validate-path/{deviceId}", requestDto);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<ValidateFilePathResponseDto>() ??
        new ValidateFilePathResponseDto(false, "Failed to deserialize response");
    });
  }

  private static async Task<string> ExtractErrorMessage(HttpResponseMessage response)
  {
    try
    {
      var content = await response.Content.ReadAsStringAsync();
      if (string.IsNullOrWhiteSpace(content))
      {
        return $"{(int)response.StatusCode} {response.ReasonPhrase}";
      }

      var mediaType = response.Content.Headers.ContentType?.MediaType;
      if (string.Equals(mediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
      {
        // Best-effort parse of RFC 7807 ProblemDetails
        if (TryGetProblemDetail(content, out var problemMessage))
        {
          return problemMessage;
        }
      }
      else
      {
        // Content might still be JSON with a `detail` field; try anyway
        if (TryGetProblemDetail(content, out var problemMessage))
        {
          return problemMessage;
        }
      }

      return content;
    }
    catch
    {
      return $"{(int)response.StatusCode} {response.ReasonPhrase}";
    }
  }

  private static bool TryGetProblemDetail(string json, out string message)
  {
    try
    {
      using var doc = JsonDocument.Parse(json);
      var root = doc.RootElement;
      if (root.ValueKind == JsonValueKind.Object)
      {
        if (root.TryGetProperty("detail", out var detailEl) && detailEl.ValueKind == JsonValueKind.String)
        {
          message = detailEl.GetString() ?? string.Empty;
          return !string.IsNullOrWhiteSpace(message);
        }

        if (root.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String)
        {
          message = titleEl.GetString() ?? string.Empty;
          return !string.IsNullOrWhiteSpace(message);
        }
      }
    }
    catch
    {
      // fall through
    }

    message = string.Empty;
    return false;
  }

  private async Task<Result> TryCallApi(Func<Task<HttpResponseMessage>> send)
  {
    try
    {
      using var response = await send.Invoke();
      if (response.IsSuccessStatusCode)
      {
        return Result.Ok();
      }

      var message = await ExtractErrorMessage(response);

      return Result.Fail(message).Log(_logger);
    }
    catch (HttpRequestException ex)
    {
      return Result
        .Fail(ex, ex.Message)
        .Log(_logger);
    }
    catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
    {
      return Result
        .Fail(ex, "The operation was canceled by the caller.")
        .Log(_logger);
    }
    catch (Exception ex)
    {
      return Result
        .Fail(ex, $"The request failed with error: {ex.Message}")
        .Log(_logger);
    }
  }

  private async Task<Result<T>> TryCallApi<T>(
    Func<Task<HttpResponseMessage>> send,
    Func<HttpResponseMessage, Task<T>> readSuccess,
    Func<HttpResponseMessage, Task<string>>? readError = null)
  {
    try
    {
      using var response = await send.Invoke();
      if (response.IsSuccessStatusCode)
      {
        var value = await readSuccess(response);
        return Result.Ok(value);
      }

      var message = readError is not null
        ? await readError(response)
        : await ExtractErrorMessage(response);

      return Result.Fail<T>(message).Log(_logger);
    }
    catch (HttpRequestException ex)
    {
      return Result
        .Fail<T>(ex, ex.Message)
        .Log(_logger);
    }
    catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
    {
      return Result
        .Fail<T>(ex, "The operation was canceled by the caller.")
        .Log(_logger);
    }
    catch (Exception ex)
    {
      return Result
        .Fail<T>(ex, $"The request failed with error: {ex.Message}")
        .Log(_logger);
    }
  }

  private async Task<Result> TryCallApi(Func<Task> func)
  {
    try
    {
      await func.Invoke();
      return Result.Ok();
    }
    catch (HttpRequestException ex)
    {
      return Result
        .Fail(ex, ex.Message)
        .Log(_logger);
    }
    catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
    {
      return Result
        .Fail(ex, "The operation was canceled by the caller.")
        .Log(_logger);
    }
    catch (Exception ex)
    {
      return Result
        .Fail(ex, "The request to the server failed.")
        .Log(_logger);
    }
  }

  private async Task<Result<T>> TryCallApi<T>(Func<Task<T?>> func)
  {
    try
    {
      var resultValue = await func.Invoke() ??
        throw new HttpRequestException("The server response was empty.");

      return Result.Ok(resultValue);
    }
    catch (HttpRequestException ex)
    {
      return Result
        .Fail<T>(ex, ex.Message)
        .Log(_logger);
    }
    catch (Exception ex)
    {
      return Result
        .Fail<T>(ex, "The request to the server failed.")
        .Log(_logger);
    }
  }

  private async Task<Result<T>> TryCallApi<T>(Func<Task<Result<T>>> func)
  {
    try
    {
      return await func.Invoke();
    }
    catch (HttpRequestException ex)
    {
      return Result
        .Fail<T>(ex, ex.Message)
        .Log(_logger);
    }
    catch (Exception ex)
    {
      return Result
        .Fail<T>(ex, "The request to the server failed.")
        .Log(_logger);
    }
  }

  private async Task<Result<T?>> TryGetNullableResponse<T>(Func<Task<T?>> func)
  {
    try
    {
      var resultValue = await func.Invoke();
      return Result.Ok(resultValue);
    }
    catch (HttpRequestException ex)
    {
      return Result
        .Fail<T?>(ex, ex.Message)
        .Log(_logger);
    }
    catch (Exception ex)
    {
      return Result
        .Fail<T?>(ex, "The request to the server failed.")
        .Log(_logger);
    }
  }
}
