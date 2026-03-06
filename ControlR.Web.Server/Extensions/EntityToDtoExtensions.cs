using System.Collections.Immutable;

namespace ControlR.Web.Server.Extensions;

public static class EntityToDtoExtensions
{
  public static BrandingSettingsDto ToDto(this BrandingSettings settings)
  {
    return new BrandingSettingsDto(
      settings.Id,
      settings.ProductName,
      settings.PrimaryColor,
      settings.SecondaryColor,
      settings.LogoFileName,
      !string.IsNullOrEmpty(settings.LogoStoragePath));
  }
  public static CreateInstallerKeyResponseDto ToCreateResponseDto(this AgentInstallerKey key, string plaintextKey)
  {
    return new CreateInstallerKeyResponseDto(
      key.Id,
      key.CreatorId,
      key.KeyType,
      plaintextKey,
      key.CreatedAt,
      key.AllowedUses,
      key.Expiration,
      key.FriendlyName);
  }
  public static AutomationSuggestionDto ToDto(this AutomationSuggestion suggestion)
  {
    return new AutomationSuggestionDto(
      suggestion.Id,
      suggestion.DeviceId,
      suggestion.SuggestionType.ToString(),
      suggestion.Title,
      suggestion.Description,
      suggestion.SuggestedScriptId,
      suggestion.Confidence,
      suggestion.Status.ToString(),
      suggestion.CreatedAt);
  }
  public static AlertDto ToDto(this Alert alert)
  {
    return new AlertDto(
      alert.Id,
      alert.AlertRuleId,
      alert.AlertRule?.Name,
      alert.DeviceId,
      alert.DeviceName,
      alert.TriggeredAt,
      alert.AcknowledgedAt,
      alert.ResolvedAt,
      alert.Status,
      alert.Details);
  }
  public static AlertRuleDto ToDto(this AlertRule rule)
  {
    return new AlertRuleDto(
      rule.Id,
      rule.Name,
      rule.MetricType,
      rule.ThresholdValue,
      rule.Operator,
      rule.Duration,
      rule.IsEnabled,
      rule.TargetDeviceIds.AsReadOnly(),
      rule.TargetGroupIds.AsReadOnly(),
      rule.NotificationRecipients,
      rule.CreatorUserId);
  }
  public static DeviceGroupDto ToDto(this DeviceGroup group)
  {
    var deviceIds = group
      .Devices?
      .Select(x => x.Id)
      .ToList() ?? [];

    var subGroupIds = group
      .SubGroups?
      .Select(x => x.Id)
      .ToList() ?? [];

    return new DeviceGroupDto(
      group.Id,
      group.Name,
      group.GroupType,
      group.Description,
      group.ParentGroupId,
      group.SortOrder,
      deviceIds,
      subGroupIds);
  }
  public static DeviceResponseDto ToDto(this Device device, bool isOutdated)
  {
    return new DeviceResponseDto(
      device.Name,
      device.AgentVersion,
      device.CpuUtilization,
      device.Id,
      device.Is64Bit,
      device.IsOnline,
      device.LastSeen,
      device.OsArchitecture,
      device.Platform,
      device.ProcessorCount,
      device.ConnectionId,
      device.OsDescription,
      device.TenantId,
      device.TotalMemory,
      device.TotalStorage,
      device.UsedMemory,
      device.UsedStorage,
      device.CurrentUsers,
      device.MacAddresses,
      device.PublicIpV4,
      device.PublicIpV6,
      device.LocalIpV4,
      device.LocalIpV6,
      device.Drives,
      isOutdated)
    {
      Alias = device.Alias,
      DeviceGroupId = device.DeviceGroupId,
      DeviceGroupName = device.DeviceGroup?.Name,
      TagIds = device.Tags?.Select(x => x.Id).ToImmutableArray()
    };
  }
  public static JitAdminAccountDto ToDto(this JitAdminAccount account)
  {
    return new JitAdminAccountDto(
      account.Id,
      account.DeviceId,
      account.DeviceName,
      account.Username,
      account.CreatedByUserId,
      account.CreatedByUserName,
      account.ExpiresAt,
      account.DeletedAt,
      (JitAdminAccountStatusDto)(int)account.Status,
      account.CreatedAt);
  }
  public static PendingPatchDto ToDto(this PendingPatch patch)
  {
    return new PendingPatchDto(
      patch.Id,
      patch.DeviceId,
      patch.UpdateId,
      patch.Title,
      patch.Description,
      patch.IsImportant,
      patch.IsCritical,
      patch.SizeBytes,
      patch.DetectedAt,
      patch.InstalledAt,
      patch.Status);
  }
  public static PatchInstallationDto ToDto(this PatchInstallation installation)
  {
    return new PatchInstallationDto(
      installation.Id,
      installation.DeviceId,
      installation.InitiatedByUserId,
      installation.InitiatedAt,
      installation.CompletedAt,
      installation.TotalCount,
      installation.InstalledCount,
      installation.FailedCount,
      installation.Status);
  }
  public static InstalledUpdateDto ToDto(this InstalledUpdate update)
  {
    return new InstalledUpdateDto(
      update.Id,
      update.DeviceId,
      update.UpdateId,
      update.Title,
      update.InstalledOn,
      update.LastReportedAt);
  }
  public static SavedScriptDto ToDto(this SavedScript script)
  {
    return new SavedScriptDto(
      script.Id,
      script.Name,
      script.Description,
      script.ScriptContent,
      script.ScriptType,
      script.CreatorUserId,
      script.IsPublishedToClients);
  }
  public static ScheduledTaskDto ToDto(this ScheduledTask task)
  {
    return new ScheduledTaskDto(
      task.Id,
      task.Name,
      task.Description,
      task.TaskType,
      task.ScriptId,
      task.Script?.Name,
      task.CronExpression,
      task.TimeZone,
      task.IsEnabled,
      task.TargetDeviceIds.AsReadOnly(),
      task.TargetGroupIds.AsReadOnly(),
      task.CreatorUserId,
      task.LastRunAt,
      task.NextRunAt);
  }
  public static ScheduledTaskExecutionDto ToDto(this ScheduledTaskExecution execution)
  {
    return new ScheduledTaskExecutionDto(
      execution.Id,
      execution.ScheduledTaskId,
      execution.ScriptExecutionId,
      execution.StartedAt,
      execution.CompletedAt,
      execution.Status);
  }
  public static ScriptExecutionDto ToDto(this ScriptExecution execution)
  {
    var results = execution.Results?
      .Select(r => r.ToDto())
      .ToList()
      .AsReadOnly() ?? (IReadOnlyList<ScriptExecutionResultDto>)[];

    return new ScriptExecutionDto(
      execution.Id,
      execution.ScriptId,
      execution.Script?.Name,
      execution.ScriptType,
      execution.InitiatedByUserId,
      execution.StartedAt,
      execution.CompletedAt,
      execution.Status,
      results);
  }
  public static ScriptExecutionResultDto ToDto(this ScriptExecutionResult result)
  {
    return new ScriptExecutionResultDto(
      result.Id,
      result.DeviceId,
      result.DeviceName,
      result.ExitCode,
      result.StandardOutput,
      result.StandardError,
      result.Status,
      result.StartedAt,
      result.CompletedAt);
  }
  public static SoftwareInventoryItemDto ToDto(this SoftwareInventoryItem item)
  {
    return new SoftwareInventoryItemDto(
      item.Id,
      item.DeviceId,
      item.Name,
      item.Version,
      item.Publisher,
      item.InstallDate,
      item.LastReportedAt);
  }
  public static RoleResponseDto ToDto(this AppRole role)
  {
    var userIds = role
      .UserRoles
      ?.Select(x => x.UserId)
      ?.ToList() ?? [];

    return new RoleResponseDto(
      role.Id,
      role.Name ?? string.Empty,
      userIds);
  }
  public static UserPreferenceResponseDto ToDto(this UserPreference userPreference)
  {
    return new UserPreferenceResponseDto(userPreference.Id, userPreference.Name, userPreference.Value);
  }
  public static ServerAlertResponseDto ToDto(this ServerAlert serverAlert)
  {
    return new ServerAlertResponseDto(
      serverAlert.Id,
      serverAlert.Message,
      serverAlert.Severity,
      serverAlert.IsDismissable,
      serverAlert.IsSticky,
      serverAlert.IsEnabled);
  }
  public static TenantSettingResponseDto ToDto(this TenantSetting tenantSetting)
  {
    return new TenantSettingResponseDto(tenantSetting.Id, tenantSetting.Name, tenantSetting.Value);
  }
  public static TagResponseDto ToDto(this Tag tag)
  {
    var userIds = tag
      .Users?
      .Select(x => x.Id)
      .ToList() ?? [];

    var deviceIds = tag
      .Devices?
      .Select(x => x.Id)
      .ToList() ?? [];

    return new TagResponseDto(
      tag.Id,
      tag.Name,
      tag.Type,
      userIds,
      deviceIds);
  }
  public static AgentInstallerKeyDto ToDto(this AgentInstallerKey key)
  {
    return new AgentInstallerKeyDto(
      key.Id,
      key.CreatorId,
      key.KeyType,
      key.CreatedAt,
      key.AllowedUses,
      key.Expiration,
      key.FriendlyName,
      key.Usages?.Select(u => new AgentInstallerKeyUsageDto(u.Id, u.DeviceId, u.CreatedAt, u.RemoteIpAddress)).ToList());
  }
  public static WebhookDeliveryLogDto ToDto(this WebhookDeliveryLog log)
  {
    return new WebhookDeliveryLogDto(
      log.Id,
      log.WebhookSubscriptionId,
      log.EventType,
      log.AttemptedAt,
      log.HttpStatusCode,
      log.IsSuccess,
      log.ResponseBody,
      log.ErrorMessage,
      log.AttemptNumber);
  }
  public static SupportSessionDto ToDto(this SupportSession session)
  {
    return new SupportSessionDto(
      session.Id,
      session.AccessCode,
      session.ClientName,
      session.ClientEmail,
      session.CreatorUserId,
      session.CreatorUserName,
      session.DeviceId,
      session.DeviceName,
      session.ExpiresAt,
      session.IsUsed,
      session.Notes,
      session.SessionStartedAt,
      session.SessionEndedAt,
      (SupportSessionStatusDto)(int)session.Status,
      session.CreatedAt);
  }

  public static SessionRecordingDto ToDto(this SessionRecording recording)
  {
    return new SessionRecordingDto(
      recording.Id,
      recording.SessionId,
      recording.DeviceId,
      recording.DeviceName,
      recording.RecorderUserId,
      recording.RecorderUserName,
      recording.SessionStartedAt,
      recording.SessionEndedAt,
      recording.DurationMs,
      recording.FrameCount,
      recording.StorageSizeBytes,
      recording.Notes,
      (SessionRecordingStatusDto)(int)recording.Status,
      recording.CreatedAt);
  }

  public static StoredCredentialDto ToDto(this StoredCredential credential)
  {
    return new StoredCredentialDto(
      credential.Id,
      credential.Name,
      credential.Description,
      credential.Username,
      credential.Domain,
      credential.DeviceId,
      credential.DeviceGroupId,
      credential.Category,
      credential.CreatedByUserId,
      credential.CreatedByUserName,
      credential.LastAccessedAt,
      credential.AccessCount,
      credential.CreatedAt);
  }

  public static ToolboxItemDto ToDto(this ToolboxItem item)
  {
    return new ToolboxItemDto(
      item.Id,
      item.Name,
      item.Description,
      item.FileName,
      item.Category,
      item.Version,
      item.FileSizeBytes,
      item.Sha256Hash,
      item.UploadedByUserId,
      item.UploadedByUserName,
      item.DeploymentCount,
      item.CreatedAt);
  }

  public static PluginRegistrationDto ToDto(
    this PluginRegistration registration,
    IReadOnlyList<LoadedPlugin> loadedPlugins)
  {
    var loaded = loadedPlugins.FirstOrDefault(p => p.RegistrationId == registration.Id);
    return new PluginRegistrationDto(
      registration.Id,
      registration.Name,
      registration.AssemblyPath,
      registration.PluginTypeName,
      registration.IsEnabled,
      registration.ConfigurationJson,
      registration.LastLoadedAt,
      loaded?.Instance.Version,
      loaded?.Instance.Description,
      registration.CreatedAt);
  }
  public static WebhookSubscriptionDto ToDto(this WebhookSubscription subscription)
  {
    return new WebhookSubscriptionDto(
      subscription.Id,
      subscription.Name,
      subscription.Url,
      subscription.EventTypes,
      subscription.IsEnabled,
      subscription.IsDisabledDueToFailures,
      subscription.FailureCount,
      subscription.LastTriggeredAt,
      subscription.LastStatus);
  }

  public static TicketingIntegrationDto ToDto(this TicketingIntegration integration)
  {
    return new TicketingIntegrationDto(
      integration.Id,
      integration.Name,
      integration.Provider,
      integration.BaseUrl,
      integration.DefaultProject,
      integration.IsEnabled,
      integration.FieldMappingJson);
  }

  public static TicketLinkDto ToDto(this TicketLink link)
  {
    return new TicketLinkDto(
      link.Id,
      link.ExternalTicketId,
      link.ExternalTicketUrl,
      link.Provider,
      link.Subject,
      link.DeviceId,
      link.SessionId,
      link.AlertId,
      link.CreatedByUserId,
      link.CreatedAt);
  }
}
