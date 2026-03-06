using ControlR.Libraries.Shared.Enums;

namespace ControlR.Libraries.Shared.Dtos.ServerApi;

[MessagePackObject(keyAsPropertyName: true)]
public record TicketingIntegrationDto(
  Guid Id,
  string Name,
  TicketingProvider Provider,
  string BaseUrl,
  string? DefaultProject,
  bool IsEnabled,
  string? FieldMappingJson) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record CreateTicketingIntegrationDto(
  string Name,
  TicketingProvider Provider,
  string BaseUrl,
  string ApiKey,
  string? DefaultProject,
  string? FieldMappingJson);

[MessagePackObject(keyAsPropertyName: true)]
public record UpdateTicketingIntegrationDto(
  Guid Id,
  string Name,
  TicketingProvider Provider,
  string BaseUrl,
  string? ApiKey, // null = don't change
  string? DefaultProject,
  bool IsEnabled,
  string? FieldMappingJson);

[MessagePackObject(keyAsPropertyName: true)]
public record TicketLinkDto(
  Guid Id,
  string ExternalTicketId,
  string ExternalTicketUrl,
  TicketingProvider Provider,
  string Subject,
  Guid? DeviceId,
  Guid? SessionId,
  Guid? AlertId,
  Guid CreatedByUserId,
  DateTimeOffset CreatedAt) : IHasPrimaryKey;

[MessagePackObject(keyAsPropertyName: true)]
public record CreateTicketRequestDto(
  Guid IntegrationId,
  string Subject,
  string Description,
  string? Priority,
  Guid? DeviceId,
  Guid? SessionId,
  Guid? AlertId);
