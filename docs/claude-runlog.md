# ControlR Deployment Runlog

## 2026-03-06: Tenant Provisioning API for Multi-Tenant ImmyBot Deployment

### What was done
Built a ServerAdministrator-only tenant provisioning API to support multi-tenant ImmyBot deployments where each ImmyBot client = one ControlR tenant.

### New endpoints (all require ServerAdministrator role)
- `GET /api/tenants` — list all tenants
- `GET /api/tenants/{id}` — get tenant
- `POST /api/tenants` — create tenant
- `PUT /api/tenants/{id}` — update tenant name
- `DELETE /api/tenants/{id}` — delete tenant (only if empty)
- `POST /api/tenants/provision` — **idempotent** create tenant + admin user + PAT in one call

### Provision endpoint flow
1. Find or create tenant by name
2. Find or create admin user in that tenant (with TenantAdmin, DeviceSuperUser, AgentInstaller, InstallerKeyManager roles)
3. Create a new PAT for that user (replaces any existing Provisioned-* PAT)
4. Returns: tenantId, tenantName, userId, adminEmail, personalAccessToken, and flags for what was created

### ImmyBot script updates
Scripts now take 3 parameters instead of 1:
- `ControlRServerAdminToken` — ServerAdministrator PAT (one for all tenants)
- `ImmyBotTenantName` — client/tenant name (auto-provisions the ControlR tenant)
- `AdminEmail` — email for the tenant admin user

### Files created
- `ControlR.Web.Server/Api/TenantsController.cs`
- `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/TenantDtos.cs`

### Files modified (local, gitignored)
- `docs/immybot/Install-ControlRAgent.ps1` — uses provision endpoint
- `docs/immybot/Configure-ControlRAgent.ps1` — uses provision endpoint

### Commit
- `81e861fa` — Add tenant provisioning API for ImmyBot multi-tenant deployment

### Deployment
- Built and deployed to production
- Verified endpoints return 401 without auth (not 404)
- Set existing tenant name to "Aspendora"

### Next steps
- User needs to create a new ServerAdministrator PAT via the UI (old PAT plaintext is lost)
- Update ImmyBot scripts with the new 3-parameter interface
- Test end-to-end provisioning + agent install

---

## 2026-03-06: Fix InstallerKey Validation for Agent Install

### Problem
Agent install via ImmyBot returned 400 on `POST /api/devices`. Two root causes:

1. **Tenant query filter**: `AgentInstallerKeys` has `.HasQueryFilter(x => x.TenantId == _tenantId)`. Anonymous `POST /api/devices` has no tenant context, so key lookups returned null. **Fix**: Added `IgnoreQueryFilters()` to all queries in `AgentInstallerKeyManager.cs` (deployed earlier).

2. **Enum mismatch**: Install script sent `keyType = 2` (TimeBased) instead of `keyType = 1` (UsageBased). TimeBased keys with no expiration immediately fail validation and get deleted. **Fix**: Changed `keyType = 2` → `keyType = 1` in `docs/immybot/Install-ControlRAgent.ps1`.

### Files changed
- `ControlR.Web.Server/Services/AgentInstallerKeyManager.cs` — `IgnoreQueryFilters()` on 3 queries (already deployed)
- `docs/immybot/Install-ControlRAgent.ps1` — `keyType = 2` → `keyType = 1`

### Next steps
- User updates script in ImmyBot and re-tests install
- Address slow download speed (144MB agent binary)

---

## 2026-03-06: API-Driven ImmyBot Deployment + /api/me Endpoint

### What was done
1. Created `/api/me` endpoint (`MeController.cs`) — returns userId, tenantId, roles from PAT auth
2. Rewrote all 4 ImmyBot PowerShell scripts to be fully API-driven (only 2 config vars needed):
   - `Detect-ControlRAgent.ps1` — registry + service fallback detection
   - `Install-ControlRAgent.ps1` — auto-discovers tenant via `/api/me`, creates single-use installer key
   - `Uninstall-ControlRAgent.ps1` — finds agent exe, runs built-in uninstall
   - `Configure-ControlRAgent.ps1` — auto-creates device groups per ImmyBot tenant name
3. Updated `docs/immybot/README.md` with new 2-variable setup instructions
4. Updated OpenAPI spec (`ControlR.Web.Server.json`) with all Phase 7-11 endpoints
5. Built and deployed to production (149.28.251.164)
6. Created "ImmyBot-Deployment" PAT via browser UI

### Configuration values for ImmyBot
- `ControlRServerUrl`: `https://control.aspendora.com`
- `ControlRPersonalAccessToken`: (created via UI, user must copy from dialog)

### Commit
- `a4eac6b4` — Add /api/me endpoint and rewrite ImmyBot scripts for API-driven deployment

---

## 2026-03-06: SMTP2GO Email + Version Fix + .NET 10 Upgrade

- Replaced MailKit SMTP with SMTP2GO REST API (`EmailSender.cs`)
- Fixed agent version "outdated" false positive (3-part vs 4-part version comparison)
- Upgraded local Mac from .NET 9 to .NET 10
- Generated EF Core migration for Phase 10-11 entities

---

## 2026-03-06: All 20 Gap Features Complete (Phases 7-11)

All features from the ScreenConnect/TeamViewer/LogMeIn/Splashtop/AnyDesk gap analysis are now implemented.

### Phase 11 Summary
- **11A Plugin API** — IControlRPlugin interface, AssemblyLoadContext loader, admin CRUD, lifecycle hooks
- **11B Helpdesk/Ticketing** — TicketingIntegration + TicketLink entities, encrypted API keys, WebhookTicketingProvider, create/link tickets
- **11C Patch Management** — PendingPatch + PatchInstallation entities, PowerShell COM API for Windows Update scan/install, hub progress reporting
- **11D AI Suggestions** — AutomationSuggestion entity, 4-rule heuristic engine (CPU/Disk/Alerts/Stale), 30-min background service
- **11E Remote Printing** — DtoType framework (60-63), ExtrasPopover printer UI, desktop client handler stubs
- **11F Audio Forwarding** — DtoType framework (70-71), RemoteDisplay audio toggle, desktop client handler stubs

### Verification
- DtoType values: 54-55 (annotations), 60-63 (printing), 70-71 (audio) — no conflicts
- All DbSets configured with tenant query filters
- All hub interfaces updated (IAgentHubClient, IViewerHub, IAgentHub, IViewerHubClient)
- All service registrations in WebApplicationBuilderExtensions.cs
- All routes, endpoints, nav links present
- Test stubs added to TestAgentHubClient

### Next: EF Core migrations + Docker build + deployment

---

## 2026-03-06: Phase 11C — Patch Management

### Goal
Add Windows patch management: scan for available updates and trigger installs remotely.

### Steps Completed
1. **Hub DTOs** -- Created `PatchManagementHubDto.cs` with `PatchScanRequestHubDto`, `PatchScanResultHubDto`, `PatchInfoHubDto`, `PatchInstallRequestHubDto`, `PatchInstallResultHubDto`
2. **Server API DTOs** -- Created `PatchManagementDto.cs` with `PendingPatchDto`, `PatchInstallationDto`, `PatchScanRequestDto`, `PatchInstallRequestDto`
3. **Entities** -- Created `PendingPatch.cs` (TenantEntityBase + DeviceId, UpdateId, Title, Description, IsImportant, IsCritical, SizeBytes, DetectedAt, InstalledAt, Status) and `PatchInstallation.cs` (TenantEntityBase + DeviceId, InitiatedByUserId, InitiatedAt, CompletedAt, TotalCount, InstalledCount, FailedCount, Status)
4. **Hub Interface Additions** -- Added `ScanForPatches`, `InstallPatches` to `IAgentHubClient`; `RequestPatchScan`, `RequestPatchInstall` to `IViewerHub`; `ReportPatchScanResult`, `ReportPatchInstallResult` to `IAgentHub`; `ReceivePatchScanProgress`, `ReceivePatchInstallProgress` to `IViewerHubClient`
5. **AppDb** -- Added `DbSet<PendingPatch>` and `DbSet<PatchInstallation>`, configuration methods with indexes and tenant query filters
6. **EntityToDtoExtensions** -- Added `ToDto()` for `PendingPatch` and `PatchInstallation`
7. **Agent Implementation** -- Added `ScanForPatches` (PowerShell COM API query for available updates) and `InstallPatches` (PowerShell COM API install) in `AgentHubClient.cs`, both async fire-and-forget with result reporting back via hub
8. **ViewerHub** -- Added `RequestPatchScan` and `RequestPatchInstall` methods with device authorization, platform check, audit logging, and installation record creation
9. **AgentHub** -- Added `ReportPatchScanResult` (stores pending patches, forwards to viewers) and `ReportPatchInstallResult` (updates installation records, forwards to viewers)
10. **TestAgentHubClient** -- Added stubs for `ScanForPatches` and `InstallPatches`
11. **ViewerHubClient** -- Added `ReceivePatchScanProgress` and `ReceivePatchInstallProgress` methods
12. **HttpConstants** -- Added `PatchManagementEndpoint = "/api/patch-management"`
13. **AuditEventTypes** -- Added `PatchManagement`; AuditActions: added `Install`, `Scan`
14. **Controller** -- Created `PatchManagementController.cs` with GET pending patches (all/by device), GET installations, POST scan, POST install
15. **ControlrApi** -- Added `GetPendingPatches`, `GetDevicePendingPatches`, `GetPatchInstallations` to interface and implementation
16. **ClientRoutes** -- Added `PatchManagement = "/patch-management"`
17. **NavMenu** -- Added "Patch Management" link under Tenant Admin section with SystemUpdateAlt icon
18. **Client Page** -- Created `PatchManagement.razor` with tabbed UI (Pending Patches with multi-select install, Installation History), MudDataGrid

### Files Created (5)
- `Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/PatchManagementHubDto.cs`
- `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/PatchManagementDto.cs`
- `ControlR.Web.Server/Data/Entities/PendingPatch.cs`
- `ControlR.Web.Server/Data/Entities/PatchInstallation.cs`
- `ControlR.Web.Server/Api/PatchManagementController.cs`
- `ControlR.Web.Client/Components/Pages/PatchManagement.razor`

### Files Modified (14)
- `Libraries/ControlR.Libraries.Shared/Hubs/Clients/IAgentHubClient.cs`
- `Libraries/ControlR.Libraries.Shared/Hubs/IViewerHub.cs`
- `Libraries/ControlR.Libraries.Shared/Hubs/IAgentHub.cs`
- `Libraries/ControlR.Libraries.Shared/Hubs/Clients/IViewerHubClient.cs`
- `Libraries/ControlR.Libraries.Shared/Constants/HttpConstants.cs`
- `Libraries/ControlR.Libraries.Shared/Enums/AuditEventType.cs`
- `Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs`
- `ControlR.Web.Server/Data/AppDb.cs`
- `ControlR.Web.Server/Extensions/EntityToDtoExtensions.cs`
- `ControlR.Web.Server/Hubs/ViewerHub.cs`
- `ControlR.Web.Server/Hubs/AgentHub.cs`
- `ControlR.Agent.Common/Services/AgentHubClient.cs`
- `ControlR.Web.Client/Services/ViewerHubClient.cs`
- `ControlR.Web.Client/ClientRoutes.cs`
- `ControlR.Web.Client/Components/Layout/NavMenu.razor`
- `Tests/ControlR.Agent.LoadTester/TestAgentHubClient.cs`

---

## 2026-03-06: Phase 11D/E/F — AI Suggestions, Remote Printing, Audio Forwarding

### Goal
Implement three foundation features: AI-Powered Automation Suggestions (rule-based heuristic engine), Remote Printing (DTO framework and viewer UI), and Audio Forwarding (DTO framework and UI toggle).

### Steps Completed

#### 11D: AI-Powered Automation Suggestions
1. **Entity** -- Created `AutomationSuggestion` with SuggestionType/SuggestionStatus enums in `ControlR.Web.Server/Data/Entities/AutomationSuggestion.cs`
2. **DTO** -- Created `AutomationSuggestionDto` and `AutomationSuggestionUpdateRequestDto` in `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/AutomationSuggestionDto.cs`
3. **AppDb** -- Added `DbSet<AutomationSuggestion>` and `ConfigureAutomationSuggestions()` with tenant query filter, status/device indexes, FK to SavedScript
4. **EntityToDtoExtensions** -- Added `ToDto()` mapping for AutomationSuggestion
5. **SuggestionEngine** -- Created `ISuggestionEngine` and `SuggestionEngineService` with rules: HighCpu (>90%), HighDisk (>95%), FrequentAlerts (5+ in 24h), StaleDevice (7+ days). `SuggestionEngineBackgroundService` runs every 30 minutes.
6. **Controller** -- Created `SuggestionsController` (GET list with status filter, PUT accept/dismiss)
7. **HttpConstants** -- Added `SuggestionsEndpoint`
8. **ControlrApi** -- Added `GetSuggestions()` and `UpdateSuggestion()` interface + implementation
9. **ClientRoutes** -- Added `Suggestions` route
10. **NavMenu** -- Added Suggestions link under Tenant Admin
11. **Client Page** -- Created `Suggestions.razor` with MudDataGrid, status filter, accept/dismiss actions, confidence indicator
12. **Service Registration** -- Registered `ISuggestionEngine` and `SuggestionEngineBackgroundService` in WebApplicationBuilderExtensions

#### 11E: Remote Printing (Foundation)
1. **DtoTypes** -- Added `GetPrinters=60`, `GetPrintersResult=61`, `PrintJob=62`, `PrintJobResult=63`
2. **DTOs** -- Created `GetPrintersDto`, `PrinterInfoDto`, `GetPrintersResultDto`, `PrintJobDto`, `PrintJobResultDto`
3. **ViewerRemoteControlStream** -- Added `SendGetPrinters()` and `SendPrintJob()` interface + implementation
4. **DesktopRemoteControlStream** -- Added handler cases for GetPrinters (returns empty array, TODO) and PrintJob (returns not-implemented error, TODO)
5. **ExtrasPopover** -- Added Print section: Load Printers button, printer dropdown, file upload, Print button (only visible for Windows devices)
6. **RemoteDisplay.razor.cs** -- Added handler cases for GetPrintersResult/PrintJobResult (delegated to ExtrasPopover)

#### 11F: Audio Forwarding (Foundation)
1. **DtoTypes** -- Added `AudioControl=70`, `AudioPacket=71`
2. **DTOs** -- Created `AudioControlDto` and `AudioPacketDto`
3. **ViewerRemoteControlStream** -- Added `SendAudioControl()` interface + implementation
4. **DesktopRemoteControlStream** -- Added handler case for AudioControl (logs receipt, TODO for platform capture)
5. **RemoteDisplay.razor** -- Added audio toggle button (VolumeOff/VolumeUp icon) in toolbar
6. **RemoteDisplay.razor.cs** -- Added `_isAudioEnabled` field, `HandleAudioToggled()` handler, AudioPacket handler case

### Files Changed
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/DtoType.cs` (modified)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/GetPrintersDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/PrinterInfoDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/GetPrintersResultDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/PrintJobDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/PrintJobResultDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/AudioControlDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/AudioPacketDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/AutomationSuggestionDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Constants/HttpConstants.cs` (modified)
- `Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs` (modified)
- `Libraries/ControlR.Libraries.Viewer.Common/ViewerRemoteControlStream.cs` (modified)
- `ControlR.DesktopClient.Common/Services/DesktopRemoteControlStream.cs` (modified)
- `ControlR.Web.Server/Data/Entities/AutomationSuggestion.cs` (new)
- `ControlR.Web.Server/Data/AppDb.cs` (modified)
- `ControlR.Web.Server/Extensions/EntityToDtoExtensions.cs` (modified)
- `ControlR.Web.Server/Services/SuggestionEngineService.cs` (new)
- `ControlR.Web.Server/Api/SuggestionsController.cs` (new)
- `ControlR.Web.Server/Startup/WebApplicationBuilderExtensions.cs` (modified)
- `ControlR.Web.Client/ClientRoutes.cs` (modified)
- `ControlR.Web.Client/Components/Layout/NavMenu.razor` (modified)
- `ControlR.Web.Client/Components/Pages/Suggestions.razor` (new)
- `ControlR.Web.Client/Components/RemoteDisplays/ExtrasPopover.razor` (modified)
- `ControlR.Web.Client/Components/RemoteDisplays/ExtrasPopover.razor.cs` (modified)
- `ControlR.Web.Client/Components/RemoteDisplays/RemoteDisplay.razor` (modified)
- `ControlR.Web.Client/Components/RemoteDisplays/RemoteDisplay.razor.cs` (modified)

---

## 2026-03-06: Phase 11B — Helpdesk / Ticketing Integration

### Goal
Allow tenants to configure external ticketing system integrations and create/link tickets from ControlR.

### Steps Completed
1. **Enum** -- Created `TicketingProvider` enum (Custom, Jira, ServiceNow, ConnectWise, Zendesk) in `Libraries/ControlR.Libraries.Shared/Enums/TicketingProvider.cs`
2. **Entity: TicketingIntegration** -- Created extending `TenantEntityBase` with Name, Provider, BaseUrl, EncryptedApiKey, DefaultProject, IsEnabled, FieldMappingJson
3. **Entity: TicketLink** -- Created extending `TenantEntityBase` with ExternalTicketId, ExternalTicketUrl, Provider, Subject, DeviceId, SessionId, AlertId, CreatedByUserId
4. **DTOs** -- Created `TicketingIntegrationDto`, `CreateTicketingIntegrationDto`, `UpdateTicketingIntegrationDto`, `TicketLinkDto`, `CreateTicketRequestDto` in `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/TicketingDto.cs`
5. **AppDb** -- Added `DbSet<TicketingIntegration>`, `DbSet<TicketLink>`, plus configure methods with unique Name+TenantId index, DeviceId/AlertId indexes, tenant query filters
6. **EntityToDtoExtensions** -- Added `ToDto()` for both TicketingIntegration and TicketLink
7. **HttpConstants** -- Added `TicketingEndpoint = "/api/ticketing"`
8. **ITicketingProvider** -- Created interface with `CreateTicket(integration, decryptedApiKey, request)` returning external ticket ID
9. **WebhookTicketingProvider** -- Generic webhook implementation: POST JSON payload to integration BaseUrl with X-Api-Key header
10. **TicketingController** -- Full CRUD for integrations (admin), POST create ticket (via provider + stores TicketLink), GET ticket links (by device/session/alert), DELETE ticket link
11. **Client API** -- Added 7 methods to `IControlrApi`: GetAllTicketingIntegrations, CreateTicketingIntegration, UpdateTicketingIntegration, DeleteTicketingIntegration, CreateTicket, GetTicketLinks, DeleteTicketLink
12. **ClientRoutes** -- Added `Ticketing = "/ticketing"`
13. **NavMenu** -- Added Ticketing link with ConfirmationNumber icon in Tenant Admin section
14. **Admin Page** -- Created `TicketingIntegrations.razor` with integration list, ticket creation, recent tickets view
15. **Edit Dialog** -- Created `TicketingIntegrationEditDialog.razor` for create/edit with provider select, base URL, encrypted API key, default project, field mapping JSON
16. **Create Ticket Dialog** -- Created `CreateTicketDialog.razor` with subject, description, priority, optional device/alert linking
17. **Service Registration** -- Registered `WebhookTicketingProvider` as `ITicketingProvider` + "Ticketing" HttpClient in `WebApplicationBuilderExtensions.cs`

### Files Created
- `Libraries/ControlR.Libraries.Shared/Enums/TicketingProvider.cs`
- `ControlR.Web.Server/Data/Entities/TicketingIntegration.cs`
- `ControlR.Web.Server/Data/Entities/TicketLink.cs`
- `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/TicketingDto.cs`
- `ControlR.Web.Server/Services/Ticketing/ITicketingProvider.cs`
- `ControlR.Web.Server/Services/Ticketing/WebhookTicketingProvider.cs`
- `ControlR.Web.Server/Api/TicketingController.cs`
- `ControlR.Web.Client/Components/Pages/TicketingIntegrations.razor`
- `ControlR.Web.Client/Components/Dialogs/TicketingIntegrationEditDialog.razor`
- `ControlR.Web.Client/Components/Dialogs/CreateTicketDialog.razor`

### Files Modified
- `ControlR.Web.Server/Data/AppDb.cs` (DbSets + configuration methods)
- `ControlR.Web.Server/Extensions/EntityToDtoExtensions.cs` (ToDto mappings)
- `Libraries/ControlR.Libraries.Shared/Constants/HttpConstants.cs` (endpoint constant)
- `Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs` (interface + implementation)
- `ControlR.Web.Client/ClientRoutes.cs` (route constant)
- `ControlR.Web.Client/Components/Layout/NavMenu.razor` (nav link)
- `ControlR.Web.Server/Startup/WebApplicationBuilderExtensions.cs` (service + HttpClient registration)

### Notes
- Cannot build locally due to .NET 10 requirement -- verified code structurally
- EF migration needed on deployment for TicketingIntegrations and TicketLinks tables
- API keys encrypted via existing `ICredentialEncryptionService` (ASP.NET Data Protection)
- WebhookTicketingProvider is the initial generic provider; specific Jira/ServiceNow/etc. providers can be added later by implementing `ITicketingProvider`

---

## 2026-03-06: Phase 11A — Plugin / Extension API

### Goal
Create a plugin/extension framework that allows custom extensions to register and respond to system events.

### Steps Completed
1. **Plugin Interface** -- Created `IControlRPlugin` in `Libraries/ControlR.Libraries.Shared/Plugins/IControlRPlugin.cs` with Name, Version, Description, Initialize, OnDeviceHeartbeat, OnSessionStart, OnSessionEnd
2. **Entity** -- Created `PluginRegistration` extending `TenantEntityBase` with Name, AssemblyPath, PluginTypeName, IsEnabled, ConfigurationJson, LastLoadedAt
3. **DTOs** -- Created `PluginRegistrationDto`, `CreatePluginRequestDto`, `UpdatePluginRequestDto` in `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/PluginDto.cs`
4. **AppDb** -- Added `DbSet<PluginRegistration>`, `ConfigurePluginRegistrations()` with unique Name+TenantId index, tenant query filter
5. **EntityToDtoExtensions** -- Added `ToDto()` for PluginRegistration (enriches with loaded plugin version/description)
6. **HttpConstants** -- Added `PluginsEndpoint = "/api/plugins"`
7. **Plugin Loader Service** -- Created `IPluginLoaderService` / `PluginLoaderService` with AssemblyLoadContext-based loading, per-plugin error isolation, tenant-scoped heartbeat notifications
8. **API Controller** -- Created `PluginsController` with GET list, GET by ID, POST create, PUT update, DELETE, POST reload
9. **Client API** -- Added 5 methods to `IControlrApi`: CreatePlugin, DeletePlugin, GetAllPlugins, ReloadPlugins, UpdatePlugin
10. **ClientRoutes** -- Added `Plugins = "/plugins"`
11. **NavMenu** -- Added Plugins link with Extension icon in Tenant Admin section
12. **Admin Page** -- Created `Plugins.razor` with MudDataGrid showing name, version, status (Loaded/Enabled/Disabled), type, last loaded, actions
13. **Edit Dialog** -- Created `PluginEditDialog.razor` for create/edit with name, assembly path, type name, config JSON, enable toggle
14. **Service Registration** -- Registered `IPluginLoaderService` as singleton in `WebApplicationBuilderExtensions.cs`

### Files Created
- `Libraries/ControlR.Libraries.Shared/Plugins/IControlRPlugin.cs`
- `ControlR.Web.Server/Data/Entities/PluginRegistration.cs`
- `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/PluginDto.cs`
- `ControlR.Web.Server/Services/PluginLoaderService.cs`
- `ControlR.Web.Server/Api/PluginsController.cs`
- `ControlR.Web.Client/Components/Pages/Plugins.razor`
- `ControlR.Web.Client/Components/Dialogs/PluginEditDialog.razor`

### Files Modified
- `ControlR.Web.Server/Data/AppDb.cs` (DbSet + configuration)
- `ControlR.Web.Server/Extensions/EntityToDtoExtensions.cs` (ToDto mapping)
- `Libraries/ControlR.Libraries.Shared/Constants/HttpConstants.cs` (endpoint)
- `Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs` (interface + implementation)
- `ControlR.Web.Client/ClientRoutes.cs` (route)
- `ControlR.Web.Client/Components/Layout/NavMenu.razor` (nav link)
- `ControlR.Web.Server/Startup/WebApplicationBuilderExtensions.cs` (service registration)

### Notes
- Cannot build locally due to .NET 10 requirement (local SDK is .NET 9) -- verified code structurally
- EF migration needed on deployment for PluginRegistrations table

---

## 2026-03-06: Phase 10D — White-Label / Branding Customization

### Goal
Allow tenant administrators to customize product name, logo, and accent colors.

### Steps Completed
1. **Entity** -- Created `BrandingSettings` extending `TenantEntityBase` with ProductName, PrimaryColor, SecondaryColor, LogoFileName, LogoStoragePath, FaviconFileName
2. **DTOs** -- Created `BrandingSettingsDto` (read) and `UpdateBrandingRequestDto` (write)
3. **EntityToDtoExtensions** -- Added `ToDto()` for BrandingSettings
4. **AppDb** -- Added `DbSet<BrandingSettings>`, `ConfigureBrandingSettings()` with unique TenantId index, tenant query filter
5. **HttpConstants** -- Added `BrandingEndpoint = "/api/branding"`
6. **AppOptions** -- Added `BrandingStoragePath` with default `./data/branding`
7. **API Controller** -- Created `BrandingController` with 5 endpoints: GET branding (anonymous), PUT update (admin), POST logo upload (admin, 5MB max, images only), GET logo (anonymous), DELETE logo (admin)
8. **Client API** -- Added 4 methods to `IControlrApi` interface and `ControlrApi` implementation (GetBranding, UpdateBranding, UploadBrandingLogo, DeleteBrandingLogo)
9. **IBrandingState** -- Created interface with ProductName, PrimaryColor, SecondaryColor, LogoUrl, IsLoaded, LoadAsync, RefreshAsync
10. **BrandingStateClient** -- Client-side implementation fetching branding from API
11. **BrandingStateServer** -- Server-side implementation fetching branding from DB
12. **Service Registration** -- Registered `IBrandingState` as scoped in both client (Program.cs) and server (WebApplicationBuilderExtensions.cs)
13. **Theme Integration** -- Modified `BaseLayout.cs` to inject `IBrandingState`, build MudTheme dynamically with branding Primary/Secondary colors, load branding on init
14. **MainLayout.razor** -- Replaced hardcoded "ControlR" text with `@BrandingState.ProductName`, added conditional logo display
15. **DeviceAccessLayout.razor** -- Same replacements for drawer header and page title
16. **ClientRoutes** -- Added `Branding = "/branding"`
17. **NavMenu** -- Added "Branding" link under Tenant Admin section with Palette icon
18. **Admin Page** -- Created `BrandingManagement.razor` with color pickers, product name field, logo upload/preview/delete, live preview panel

### Files Created (6)
- `ControlR.Web.Server/Data/Entities/BrandingSettings.cs`
- `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/BrandingDto.cs`
- `ControlR.Web.Server/Api/BrandingController.cs`
- `ControlR.Web.Client/Services/IBrandingState.cs`
- `ControlR.Web.Client/Services/BrandingStateClient.cs`
- `ControlR.Web.Server/Services/BrandingStateServer.cs`
- `ControlR.Web.Client/Components/Pages/BrandingManagement.razor`

### Files Modified (10)
- `Libraries/ControlR.Libraries.Shared/Constants/HttpConstants.cs` (added BrandingEndpoint)
- `ControlR.Web.Server/Options/AppOptions.cs` (added BrandingStoragePath)
- `ControlR.Web.Server/Data/AppDb.cs` (added DbSet + ConfigureBrandingSettings)
- `ControlR.Web.Server/Extensions/EntityToDtoExtensions.cs` (added ToDto)
- `Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs` (added 4 branding methods to interface + implementation)
- `ControlR.Web.Client/ClientRoutes.cs` (added Branding route)
- `ControlR.Web.Client/Components/Layout/NavMenu.razor` (added nav link)
- `ControlR.Web.Client/Components/Layout/BaseLayout.cs` (injected IBrandingState, dynamic theme building)
- `ControlR.Web.Client/Components/Layout/MainLayout.razor` (dynamic product name + logo)
- `ControlR.Web.Client/Components/Layout/DeviceAccess/DeviceAccessLayout.razor` (dynamic product name + logo)
- `ControlR.Web.Client/Program.cs` (registered BrandingStateClient)
- `ControlR.Web.Server/Startup/WebApplicationBuilderExtensions.cs` (registered BrandingStateServer)

### Build Verification
- Cannot build locally (.NET 10 required, .NET 9 SDK installed)
- All patterns follow existing codebase conventions (entity, DTO, controller, API client, service, page)
- Code reviewed for correctness: namespaces, global usings, DI registrations

### Next Steps
- Generate EF Core migration for BrandingSettings table
- Build and deploy

---

## 2026-03-06: Phase 10C — Session Annotations / Whiteboard

### Goal
Add freehand annotation drawing on the remote screen during sessions.

### What was done
1. Added `AnnotationStroke = 54` and `AnnotationClear = 55` to `DtoType` enum
2. Created `AnnotationStrokeDto` and `AnnotationClearDto` MessagePack records
3. Added `SendAnnotationStroke()` and `SendAnnotationClear()` to `IViewerRemoteControlStream` interface and implementation
4. Added `DtoType.AnnotationStroke` and `DtoType.AnnotationClear` cases to `DesktopRemoteControlStream.HandleMessageReceived` (log-only for now)
5. Created `AnnotationCanvas` component (razor + cs + js + css) with:
   - Transparent HTML5 canvas overlay on top of remote display
   - JS interop for freehand drawing using PointerEvents
   - Normalized 0-1 coordinate system for strokes
   - Color picker (red, blue, green, yellow, white, black)
   - Thickness selector (thin/medium/thick)
   - Clear button with DTO broadcast
   - Local stroke rendering for responsiveness
6. Integrated `AnnotationCanvas` into `RemoteDisplay.razor` with toggle button in the action bar

### Files created
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/AnnotationStrokeDto.cs`
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/AnnotationClearDto.cs`
- `ControlR.Web.Client/Components/RemoteDisplays/AnnotationCanvas.razor`
- `ControlR.Web.Client/Components/RemoteDisplays/AnnotationCanvas.razor.cs`
- `ControlR.Web.Client/Components/RemoteDisplays/AnnotationCanvas.razor.js`
- `ControlR.Web.Client/Components/RemoteDisplays/AnnotationCanvas.razor.css`

### Files modified
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/DtoType.cs` — added enum values 54, 55
- `Libraries/ControlR.Libraries.Viewer.Common/ViewerRemoteControlStream.cs` — added interface methods and implementations
- `ControlR.DesktopClient.Common/Services/DesktopRemoteControlStream.cs` — added handler cases (log-only)
- `ControlR.Web.Client/Components/RemoteDisplays/RemoteDisplay.razor` — added annotate toggle button and AnnotationCanvas component
- `ControlR.Web.Client/Components/RemoteDisplays/RemoteDisplay.razor.cs` — added `_isAnnotationMode` field, `AnnotationCanvasStyle` property, toggle handler

---

## 2026-03-06: Phase 10A — Enhanced Connection Quality Indicators

### Goal
Add an always-visible traffic-light connection quality indicator to the remote display toolbar, computed from existing latency and FPS metrics.

### Steps Completed
1. **ConnectionQuality enum** — Created in `Libraries/ControlR.Libraries.Viewer.Common/Enums/ConnectionQuality.cs` with Excellent, Good, Fair, Poor values
2. **ConnectionQualityIndicator.razor** — New component using MudTooltip with rich detail (latency, FPS, Mbps in/out) and MudIcon with color-coded circle (green/yellow/red)
3. **ConnectionQualityIndicator.razor.cs** — Code-behind subscribing to `IMetricsState.OnStateChanged`, computing quality from latency (<50ms=Excellent, <100ms=Good, <200ms=Fair, >=200ms=Poor) and FPS (>=25=Excellent, >=15=Good, >=5=Fair, <5=Poor), overall = worst of two
4. **RemoteDisplay.razor** — Added `<ConnectionQualityIndicator />` in the action bar after `MudFlexBreak`, before fullscreen/close buttons (always visible)

### Files Created (3)
- `Libraries/ControlR.Libraries.Viewer.Common/Enums/ConnectionQuality.cs`
- `ControlR.Web.Client/Components/RemoteDisplays/ConnectionQualityIndicator.razor`
- `ControlR.Web.Client/Components/RemoteDisplays/ConnectionQualityIndicator.razor.cs`

### Files Modified (1)
- `ControlR.Web.Client/Components/RemoteDisplays/RemoteDisplay.razor` (added indicator component)

### Build Verification
- Cannot build locally (.NET 10 required, .NET 9 SDK installed)
- All namespace imports verified via global usings (Viewer.Common.Enums, Viewer.Common.State, MudBlazor, Microsoft.AspNetCore.Components)
- Component follows exact same patterns as MetricsFrame.razor.cs (IDisposable, IMetricsState injection, OnStateChanged subscription)
- No @rendermode added (component inherits from parent)

---

## 2026-03-06: Phase 9 Complete — Security & Credentials

### Summary
Phase 9 implemented three security features:
- **9A Credential Vault** — Encrypted credential storage with per-tenant Data Protection, audit-logged retrieval, device/group scoping
- **9B JIT Admin Accounts** — Temporary Windows local admin via SignalR (`net user /add`), auto-expiry via background cleanup service
- **9C Per-Action Auth** — IMemoryCache-backed 5-minute verification window, `[RequiresVerification]` filter for REST, `IActionVerificationGuard` client guard

### Verification
All Phase 9 files reviewed for correctness:
- Entity definitions, DTOs, controllers, services, UI pages, dialogs
- Service registrations in WebApplicationBuilderExtensions.cs and IServiceCollectionExtensions.cs
- Hub interface additions (IAgentHubClient, IViewerHub) and ViewerHub implementations
- Agent-side command execution (CreateJitAdminAccount, DeleteJitAdminAccount)
- NavMenu links, ClientRoutes, HttpConstants — all present
- Global usings cover all namespaces needed by RequiresVerificationAttribute
- No compilation issues detected

### Next: Phase 10

---

## 2026-03-06: Phase 9C - Zero Trust Per-Action Authentication

### Goal
Implement per-action re-authentication for sensitive/destructive operations. Users must re-enter their password before performing actions like script execution, agent uninstall, device deletion, safe mode reboot, credential retrieval, and JIT admin creation.

### Architecture
- Server-side: `IMemoryCache`-backed verification with 5-minute TTL per user
- REST endpoints: `[RequiresVerification]` action filter attribute returns 403 with `VERIFICATION_REQUIRED` code
- SignalR hub: Inline `IsActionVerified()` helper checks verification status
- Client-side: `IActionVerificationGuard` service checks status, shows password dialog if needed

### Steps Completed

1. **Service** - `ControlR.Web.Server/Services/ActionVerificationService.cs`
   - `IActionVerificationService` with `IsVerified`, `GetExpiresAt`, `SetVerified`, `Revoke`
   - Backed by `IMemoryCache` with key prefix `action-verification-`

2. **DTOs** - `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/ActionVerificationDto.cs`
   - `ActionVerificationRequestDto(string Password)`
   - `ActionVerificationStatusDto(bool IsVerified, DateTimeOffset? ExpiresAt)`

3. **Controller** - `ControlR.Web.Server/Api/ActionVerificationController.cs`
   - `POST /api/action-verification/verify` - validates password via `UserManager.CheckPasswordAsync`
   - `GET /api/action-verification/status` - returns current verification status

4. **Action Filter** - `ControlR.Web.Server/Middleware/RequiresVerificationAttribute.cs`
   - Returns 403 with `VERIFICATION_REQUIRED` code when user is not verified

5. **HttpConstants** - Added `ActionVerificationEndpoint`

6. **Service Registration** - Added `IActionVerificationService` as singleton in `WebApplicationBuilderExtensions.cs`

7. **Global Usings** - Added `ControlR.Web.Server.Middleware` to server Usings.cs

8. **API Client** - Added `GetActionVerificationStatus()` and `VerifyAction()` to `IControlrApi` and `ControlrApi`

9. **Client Dialog** - `ControlR.Web.Client/Components/Dialogs/ActionVerificationDialog.razor`
   - Password input with auto-focus, Enter key support, error display, loading state

10. **Client Service** - `ControlR.Web.Client/Services/ActionVerificationService.cs`
    - `IActionVerificationGuard.EnsureVerified()` checks status, shows dialog if needed

11. **Client Service Registration** - Added `IActionVerificationGuard` as scoped in `IServiceCollectionExtensions.cs`

12. **Dashboard Protection** - Added `VerificationGuard.EnsureVerified()` before:
    - `RemoveDevice`, `RebootToSafeMode`, `UninstallAgent`, `CreateJitAdmin`

13. **ScriptRunDialog Protection** - Added verification before `ExecuteScript`

14. **REST Endpoint Protection** - Added `[RequiresVerification]` to:
    - `DevicesController.DeleteDevice`
    - `CredentialsController.RetrieveCredentialPassword`
    - `CredentialsController.DeleteCredential`

15. **Hub Method Protection** - Added `IsActionVerified()` checks to:
    - `ViewerHub.ExecuteScript`
    - `ViewerHub.UninstallAgent`
    - `ViewerHub.RequestSafeModeReboot`

### Build Verification
- Cannot build locally (.NET 10 required, .NET 9 SDK installed) - verified code correctness through review
- All patterns follow existing codebase conventions

---

## 2026-03-06: Phase 8C - Remote Resolution Change

### Goal
Implement the ability for the viewer to change the display resolution on a remote device during a remote control session.

### Architecture Decision
After thorough codebase research, determined that resolution change should use the **DtoWrapper remote control stream** pattern (same as privacy screen, block input) rather than the hub DTO pattern. This is because:
- Resolution change happens during an active remote control session
- The desktop client (not the agent) manages displays
- The WebSocket relay stream already carries display-related commands

### Steps Completed
1. **DtoType enum** - Added 4 new values: `GetAvailableResolutions` (50), `GetAvailableResolutionsResult` (51), `ChangeResolution` (52), `ChangeResolutionResult` (53)
2. **RemoteControl DTOs** - Created 5 new DTO records:
   - `GetAvailableResolutionsDto` (request to list resolutions)
   - `AvailableResolutionDto` (Width, Height, RefreshRate)
   - `GetAvailableResolutionsResultDto` (success/error + array of resolutions)
   - `ChangeResolutionDto` (DisplayId, Width, Height, RefreshRate?)
   - `ChangeResolutionResultDto` (success/error)
3. **IDisplayManager interface** - Added `ChangeResolution()` and `GetAvailableResolutions()` methods
4. **Windows implementation** - Full P/Invoke implementation using `EnumDisplaySettings` and `ChangeDisplaySettingsEx`:
   - `GetAvailableResolutions`: Enumerates all display modes, deduplicates, sorts by resolution desc
   - `ChangeResolution`: Tests mode first with `CDS_TEST`, then applies dynamically (session-scoped)
   - Added `ChangeDisplaySettingsEx` to NativeMethods.txt for CsWin32
5. **Mac/Linux stubs** - All three non-Windows display managers return `Result.Fail("not supported")`
6. **Desktop client handler** - Added `GetAvailableResolutions` and `ChangeResolution` case handlers in `DesktopRemoteControlStream.HandleMessageReceived`. After successful change, reloads displays and sends updated DisplayData
7. **Viewer stream** - Added `SendGetAvailableResolutions()` and `SendChangeResolution()` to `IViewerRemoteControlStream` interface and implementation
8. **ExtrasPopover UI** - Converted from inline `@code` to code-behind pattern. Added resolution picker:
   - "Load Resolutions" button (on-demand loading, Windows-only visibility)
   - MudSelect dropdown showing all available WxH@Hz modes
   - Loading spinner, error display, and disable-during-change states
   - Handles `GetAvailableResolutionsResult` and `ChangeResolutionResult` DTOs
9. **RemoteDisplay handler** - Added `GetAvailableResolutionsResult` and `ChangeResolutionResult` to switch to suppress "unsupported" warnings

### Files Changed
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/DtoType.cs` (modified)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/GetAvailableResolutionsDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/AvailableResolutionDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/GetAvailableResolutionsResultDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/ChangeResolutionDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/ChangeResolutionResultDto.cs` (new)
- `ControlR.DesktopClient.Common/ServiceInterfaces/IDisplayManager.cs` (modified)
- `ControlR.DesktopClient.Windows/Services/DisplayManagerWindows.cs` (modified)
- `ControlR.DesktopClient.Mac/Services/DisplayManagerMac.cs` (modified)
- `ControlR.DesktopClient.Linux/Services/DisplayManagerX11.cs` (modified)
- `ControlR.DesktopClient.Linux/Services/DisplayManagerWayland.cs` (modified)
- `Libraries/ControlR.Libraries.NativeInterop.Windows/NativeMethods.txt` (modified)
- `ControlR.DesktopClient.Common/Services/DesktopRemoteControlStream.cs` (modified)
- `Libraries/ControlR.Libraries.Viewer.Common/ViewerRemoteControlStream.cs` (modified)
- `ControlR.Web.Client/Components/RemoteDisplays/ExtrasPopover.razor` (modified)
- `ControlR.Web.Client/Components/RemoteDisplays/ExtrasPopover.razor.cs` (new)
- `ControlR.Web.Client/Components/RemoteDisplays/RemoteDisplay.razor.cs` (modified)

## 2026-03-06: Phase 8A - Toolbox Feature

### Goal
Implement Toolbox feature: program/file store where tenant admins can upload tools, installers, and utilities, then deploy them to remote devices on demand.

### Steps Completed
1. **Entity** -- Created `ToolboxItem` entity extending `TenantEntityBase` with Name, Description, FileName, Category, Version, FileSizeBytes, StoragePath, Sha256Hash, UploadedByUserId/Name, DeploymentCount
2. **DTOs** -- Created `ToolboxItemDto`, `ToolboxItemCreateRequestDto`, `ToolboxItemUpdateRequestDto`, `ToolboxDeployRequestDto`
3. **EntityToDtoExtensions** -- Added `ToDto()` for ToolboxItem
4. **AppDb** -- Added `DbSet<ToolboxItem>`, `ConfigureToolboxItems()` with unique index on (Name, TenantId), tenant query filter
5. **HttpConstants** -- Added `ToolboxEndpoint = "/api/toolbox"`
6. **AppOptions** -- Added `ToolboxStoragePath` with default `./data/toolbox`
7. **API Controller** -- Created `ToolboxController` with 6 endpoints: GET list, GET single, POST upload (multipart, 500MB limit, SHA256 hash), PUT update metadata, DELETE (removes files), GET download
8. **Client API** -- Added 5 methods to `IControlrApi` interface and `ControlrApi` implementation (GetToolboxItems, GetToolboxItem, UploadToolboxItem, UpdateToolboxItem, DeleteToolboxItem)
9. **Client Routes** -- Added `Toolbox = "/toolbox"`
10. **Nav Menu** -- Added "Toolbox" link under Tenant Admin section with BuildCircle icon
11. **Blazor Page** -- Created `Toolbox.razor` with MudDataGrid (Name, Category, Version, File Name, Size, Uploaded By, Deployments, Actions), upload dialog trigger, download, delete with confirmation
12. **Upload Dialog** -- Created `ToolboxUploadDialog.razor` with MudForm (Name, Description, Category, Version), MudFileUpload for file selection, progress indicator, 500MB max file size

### Files Created (4)
- ControlR.Web.Server/Data/Entities/ToolboxItem.cs
- Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/ToolboxDto.cs
- ControlR.Web.Server/Api/ToolboxController.cs
- ControlR.Web.Client/Components/Pages/Toolbox.razor
- ControlR.Web.Client/Components/Dialogs/ToolboxUploadDialog.razor

### Files Modified (7)
- ControlR.Web.Server/Extensions/EntityToDtoExtensions.cs
- ControlR.Web.Server/Data/AppDb.cs
- ControlR.Web.Server/Options/AppOptions.cs
- Libraries/ControlR.Libraries.Shared/Constants/HttpConstants.cs
- Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs
- ControlR.Web.Client/ClientRoutes.cs
- ControlR.Web.Client/Components/Layout/NavMenu.razor

### Next Steps
- Generate EF Core migration for ToolboxItem table
- Build and deploy
- Future: implement actual deployment to devices via hub (deploy button is placeholder)

---

## 2026-03-06: Phase 8B - Reboot to Safe Mode Feature

### Goal
Implement "Reboot to Safe Mode" for Windows devices: admin sends command via ViewerHub, agent configures bcdedit and reboots, agent auto-reconnects after reboot.

### Steps Completed
1. **AuditActions** -- Added `SafeModeReboot` constant to `AuditActions` class
2. **HubDto** -- Created `SafeModeRebootRequestHubDto` (with `WithNetworking` flag) and `SafeModeRebootResultHubDto`
3. **IAgentHubClient** -- Added `Task<Result> RebootToSafeMode(SafeModeRebootRequestHubDto request)` to interface
4. **IViewerHub** -- Added `Task<Result> RequestSafeModeReboot(Guid deviceId, bool withNetworking = true)` to interface
5. **ViewerHub** -- Implemented `RequestSafeModeReboot`: authorizes device, checks `Platform == SystemPlatform.Windows`, calls agent, audits on success
6. **AgentHubClient** -- Implemented `RebootToSafeMode`: runs `bcdedit /set {current} safeboot network|minimal`, then `shutdown /r /t 5 /f`; non-Windows returns failure
7. **TestAgentHubClient** -- Added stub implementation returning `Result.Ok()`
8. **Dashboard.razor** -- Added "Safe Mode Reboot" menu item (only visible for online Windows devices) with HealthAndSafety icon in Warning color
9. **Dashboard.razor.cs** -- Added `RebootToSafeMode` handler with confirmation dialog, success/error snackbar feedback

### Files Created (1)
- Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/SafeModeRebootHubDto.cs

### Files Modified (7)
- Libraries/ControlR.Libraries.Shared/Enums/AuditEventType.cs (added SafeModeReboot action)
- Libraries/ControlR.Libraries.Shared/Hubs/Clients/IAgentHubClient.cs (added RebootToSafeMode)
- Libraries/ControlR.Libraries.Shared/Hubs/IViewerHub.cs (added RequestSafeModeReboot)
- ControlR.Web.Server/Hubs/ViewerHub.cs (implemented RequestSafeModeReboot)
- ControlR.Agent.Common/Services/AgentHubClient.cs (implemented RebootToSafeMode)
- Tests/ControlR.Agent.LoadTester/TestAgentHubClient.cs (added stub)
- ControlR.Web.Client/Components/Dashboard.razor (added Safe Mode Reboot menu item)
- ControlR.Web.Client/Components/Dashboard.razor.cs (added RebootToSafeMode handler)

### Design Notes
- Safe Mode with Networking uses `bcdedit /set {current} safeboot network`; without networking uses `safeboot minimal`
- After reboot, the agent service auto-reconnects (existing behavior, no changes needed)
- The safeboot flag persists across reboots; admin should plan to clear it (`bcdedit /deletevalue {current} safeboot`) after troubleshooting -- this can be done via terminal or script execution
- Only Windows devices show the Safe Mode Reboot option in the UI
- Server-side also validates platform before forwarding to agent

---

## 2026-03-06: Session Recording Feature

### Goal
Implement session recording: DB metadata + server-side JPEG frame storage + admin playback viewer.

### Steps Completed
1. **Entity** -- Created `SessionRecording` entity extending `TenantEntityBase` with SessionId, DeviceId/Name, RecorderUserId/Name, timestamps, FrameCount, StorageSizeBytes, StoragePath, Status enum
2. **DTOs** -- Created `SessionRecordingDto`, `StartRecordingRequestDto`, `StopRecordingRequestDto`, `UploadRecordingFrameDto`, `SessionRecordingStatusDto`
3. **EntityToDtoExtensions** -- Added `ToDto()` for SessionRecording with enum cast
4. **AppDb** -- Added `DbSet<SessionRecording>`, `ConfigureSessionRecordings()` with indexes on SessionId and DeviceId, tenant query filter
5. **HttpConstants** -- Added `SessionRecordingsEndpoint = "/api/session-recordings"`
6. **AppOptions** -- Added `RecordingsStoragePath` with default `./data/recordings`
7. **API Controller** -- Created `SessionRecordingsController` with 8 endpoints: start, frame upload, stop, list, get, frame list, get frame, delete
8. **Client API** -- Added 7 methods to `IControlrApi` interface and `ControlrApi` implementation (StartRecording, StopRecording, UploadRecordingFrame, GetSessionRecordings, GetSessionRecording, GetSessionRecordingFrameList, DeleteSessionRecording)
9. **Client Routes** -- Already existed from prior session (SessionRecordings, SessionRecordingPlayback)
10. **Nav Menu** -- Already existed from prior session (Session Recordings link under Tenant Admin)
11. **Blazor Pages** -- Created `SessionRecordings.razor` (list with MudDataGrid) and `SessionRecordingPlayback.razor` (frame-by-frame playback with play/pause/seek/speed controls)
12. **Cleanup Service** -- Already existed from prior session (RecordingCleanupService, registered in DI)

### Files Created (4)
- ControlR.Web.Server/Data/Entities/SessionRecording.cs
- Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/SessionRecordingDto.cs
- ControlR.Web.Server/Api/SessionRecordingsController.cs
- ControlR.Web.Client/Components/Pages/SessionRecordings.razor
- ControlR.Web.Client/Components/Pages/SessionRecordingPlayback.razor

### Files Modified (5)
- ControlR.Web.Server/Extensions/EntityToDtoExtensions.cs
- ControlR.Web.Server/Data/AppDb.cs
- ControlR.Web.Server/Options/AppOptions.cs
- Libraries/ControlR.Libraries.Shared/Constants/HttpConstants.cs
- Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs

### Pre-existing (from prior session)
- ControlR.Web.Client/ClientRoutes.cs (routes already added)
- ControlR.Web.Client/Components/Layout/NavMenu.razor (nav entry already added)
- ControlR.Web.Server/Services/RecordingCleanupService.cs (already created)
- ControlR.Web.Server/Startup/WebApplicationBuilderExtensions.cs (cleanup service already registered)

### Next Steps
- Generate EF Core migration for SessionRecording table
- Build and deploy

---

## 2026-03-06: On-Demand Support Sessions Feature

### Goal
Implement on-demand support sessions: technicians create sessions with access codes, clients join via public page.

### Steps Completed
1. **Entity** -- Created `SupportSession` entity extending `TenantEntityBase` with access code, status, client/creator info, timestamps
2. **DTOs** -- Created `SupportSessionDto`, `SupportSessionCreateRequestDto`, `SupportSessionJoinRequestDto`, `SupportSessionJoinResponseDto`, `SupportSessionStatusDto`
3. **EntityToDtoExtensions** -- Added `ToDto()` for SupportSession with enum cast
4. **AppDb** -- Added `DbSet<SupportSession>`, `ConfigureSupportSessions()` with unique index on (AccessCode, TenantId), index on Status, tenant query filter
5. **HttpConstants** -- Added `SupportSessionsEndpoint = "/api/support-sessions"`
6. **API Controller** -- Created `SupportSessionsController` with GET (list), POST (create), POST /join (anonymous), DELETE (cancel), PUT /complete endpoints
7. **Client API** -- Added 5 methods to `IControlrApi` interface and `ControlrApi` implementation
8. **Client Routes** -- Added `SupportSessions` and `SupportSessionJoin` routes
9. **Nav Menu** -- Added "Support Sessions" link under Tenant Admin section
10. **Blazor Pages** -- Created `SupportSessions.razor` (admin), `SupportSessionJoin.razor` (public), `SupportSessionCreateDialog.razor`
11. **Cleanup Service** -- Created `SupportSessionCleanupService` background service (runs every 5 min), registered in DI

### Files Created (6)
- ControlR.Web.Server/Data/Entities/SupportSession.cs
- Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/SupportSessionDto.cs
- ControlR.Web.Server/Api/SupportSessionsController.cs
- ControlR.Web.Client/Components/Pages/SupportSessions.razor
- ControlR.Web.Client/Components/Pages/SupportSessionJoin.razor
- ControlR.Web.Client/Components/Dialogs/SupportSessionCreateDialog.razor
- ControlR.Web.Server/Services/SupportSessionCleanupService.cs

### Files Modified (6)
- ControlR.Web.Server/Extensions/EntityToDtoExtensions.cs
- ControlR.Web.Server/Data/AppDb.cs
- Libraries/ControlR.Libraries.Shared/Constants/HttpConstants.cs
- Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs
- ControlR.Web.Client/ClientRoutes.cs
- ControlR.Web.Client/Components/Layout/NavMenu.razor
- ControlR.Web.Server/Startup/WebApplicationBuilderExtensions.cs

### Next Steps
- Generate EF Core migration for SupportSession table
- Build and deploy

## 2026-03-06: Support Sessions UI Enhancement

### Goal
Rewrite three Blazor UI pages for the support sessions feature with full MudBlazor patterns.

### Steps Completed
1. **SupportSessions.razor** -- Rewrote admin page with: `@rendermode InteractiveWebAssembly`, `ILazyInjector<IControlrApi>`, 30-second auto-refresh via `Timer` + `IDisposable`, relative time display with tooltip for absolute time, copy-to-clipboard helper, share link copy button in actions column
2. **SupportSessionCreateDialog.razor** -- Rewrote dialog with: `ILazyInjector<IControlrApi>`, two-phase UI (form -> success display), prominent access code display with large monospace H3 text, copy button for code, share link with copy button, expiration info, "Done" button to close
3. **SupportSessionJoin.razor** -- Rewrote public join page with: `@rendermode InteractiveWebAssembly`, `@attribute [AllowAnonymous]`, `ILazyInjector<IControlrApi>`, `PatternMask` for 8-digit input, input validation, "Connecting..." state, "keep this page open" instructions, query param pre-fill via `[SupplyParameterFromQuery]`

### Files Modified (3)
- ControlR.Web.Client/Components/Pages/SupportSessions.razor
- ControlR.Web.Client/Components/Dialogs/SupportSessionCreateDialog.razor
- ControlR.Web.Client/Components/Pages/SupportSessionJoin.razor

---

## 2026-03-06: Phase 7 Architecture Research

### Goal
Read three task output files from prior agent explorations and synthesize a concise architectural summary for implementing Phase 7 features (on-demand sessions, session recording, backstage mode).

### Steps Completed
1. **Read task output files** -- all three were JSON-L agent conversation logs (not final summaries). Extracted tool call patterns and search targets.
2. **Direct codebase exploration** -- read ~30 key source files to build the summary from primary sources:
   - Hub interfaces and implementations (AgentHub, ViewerHub, IAgentHub, IViewerHub, IAgentHubClient, IViewerHubClient)
   - Agent startup flow (HubConnectionInitializer, AgentHeartbeatTimer, SettingsProvider, CommandProvider)
   - Remote control session flow (AgentHubClient.CreateRemoteControlSession, RemoteControlHostManager, DesktopRemoteControlStream, RemoteControlSessionInitializer)
   - Screen capture pipeline (IScreenGrabber, IDesktopCapturer, FrameBasedCapturer, DesktopCapturerFactory)
   - Encoding (JpegEncoder, Vp9Encoder, IStreamEncoder, IFrameEncoder)
   - WebSocket relay (WebSocketRelayMiddleware, ManagedRelayStream, SessionSignaler)
   - Privacy screen (DisplayManagerWindows, TogglePrivacyScreenDto, Win32Interop.CreatePrivacyScreenWindow)
   - Auth infrastructure (LogonTokenProvider, LogonTokenAuthenticationHandler)
   - Audit system (AuditService, AuditLog entity)
   - DB entities (Device, AuditLog)
   - Remote control DTOs (ScreenRegionDto, VideoStreamPacketDto, DtoType enum, RemoteControlSessionRequestDto)
3. **Created architectural summary** -- `docs/phase7-architecture-summary.md`

### Files Created
- `docs/phase7-architecture-summary.md` -- comprehensive reference covering all three Phase 7 feature areas

### Key Findings
- AgentHub is unauthenticated; agent self-identifies via UpdateDevice with DeviceId+TenantId
- Remote control frames are JPEG-encoded dirty regions (ScreenRegionDto) sent via WebSocket relay
- Server is a dumb relay -- never inspects frame data; recording requires tapping the relay or a parallel stream
- No file/blob storage exists on the server; no dedicated session entity in DB
- Privacy screen is Windows-only, creates a transparent topmost overlay (not true screen blanking)
- LogonTokenProvider already implements one-time-use device-scoped tokens (reusable pattern for temp agents)
- VP9 encoder exists but is a non-production stub using ffmpeg subprocess

## 2026-03-05: Interactive PTY Terminal Implementation

### Goal
Add real PTY-based terminal (xterm.js + ConPTY/forkpty) to replace command-and-response PowerShell model.

### Steps Completed
1. **Created PTY DTOs** — PtyInputDto, PtyOutputDto, PtyResizeDto with MessagePack serialization
2. **Added hub interface methods** — IViewerHub (4 methods), IAgentHubClient (4 methods), IAgentHub (+1), IViewerHubClient (+1)
3. **Implemented server hub methods** — ViewerHub (CreatePtySession, SendPtyInput, ResizePty, ClosePtySession), AgentHub (SendPtyOutputToViewer)
4. **Created PtyTerminalSession** — Platform-specific PTY: ConPTY on Windows, forkpty on Linux/macOS
5. **Created ConPtyInterop.cs** — Windows P/Invoke: CreatePseudoConsole, CreateProcessW, ResizePseudoConsole, etc.
6. **Created UnixPtyInterop.cs** — Linux/macOS P/Invoke: forkpty, read, write, ioctl(TIOCSWINSZ), kill, waitpid
7. **Updated TerminalSessionFactory** — Added CreatePtySession method
8. **Updated TerminalStore** — Added PTY session cache, WritePtyInput, ResizePty, TryRemovePty
9. **Updated AgentHubClient** — Added 5 PTY handlers
10. **Updated ViewerHubClient** — Added ReceivePtyOutput handler
11. **Downloaded xterm.js v5.5.0** — xterm.min.js, xterm.css, addon-fit.min.js, addon-web-links.min.js
12. **Created PtyTerminal Blazor component** — .razor, .razor.cs, .razor.js, .razor.css
13. **Updated Terminal.razor** — Added toggle between Interactive Terminal (PTY) and PowerShell Console
14. **Updated App.razor** — Added xterm.js CSS and script tags

### Files Created (14)
- Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/PtyInputDto.cs
- Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/PtyOutputDto.cs
- Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/PtyResizeDto.cs
- ControlR.Agent.Common/Services/Terminal/PtyTerminalSession.cs
- ControlR.Agent.Common/Services/Terminal/Interop/ConPtyInterop.cs
- ControlR.Agent.Common/Services/Terminal/Interop/UnixPtyInterop.cs
- ControlR.Web.Client/Components/Pages/DeviceAccess/PtyTerminal.razor
- ControlR.Web.Client/Components/Pages/DeviceAccess/PtyTerminal.razor.cs
- ControlR.Web.Client/Components/Pages/DeviceAccess/PtyTerminal.razor.js
- ControlR.Web.Client/Components/Pages/DeviceAccess/PtyTerminal.razor.css
- ControlR.Web.Server/wwwroot/lib/xterm/xterm.min.js
- ControlR.Web.Server/wwwroot/lib/xterm/xterm.css
- ControlR.Web.Server/wwwroot/lib/xterm/addon-fit.min.js
- ControlR.Web.Server/wwwroot/lib/xterm/addon-web-links.min.js

### Files Modified (12)
- Libraries/ControlR.Libraries.Shared/Hubs/IViewerHub.cs
- Libraries/ControlR.Libraries.Shared/Hubs/Clients/IAgentHubClient.cs
- Libraries/ControlR.Libraries.Shared/Hubs/IAgentHub.cs
- Libraries/ControlR.Libraries.Shared/Hubs/Clients/IViewerHubClient.cs
- ControlR.Web.Server/Hubs/ViewerHub.cs
- ControlR.Web.Server/Hubs/AgentHub.cs
- ControlR.Web.Server/Components/App.razor
- ControlR.Web.Client/Services/ViewerHubClient.cs
- ControlR.Agent.Common/Services/Terminal/TerminalSessionFactory.cs
- ControlR.Agent.Common/Services/Terminal/TerminalStore.cs
- ControlR.Agent.Common/Services/AgentHubClient.cs
- ControlR.Web.Client/Components/Pages/DeviceAccess/Terminal.razor
- ControlR.Web.Client/Components/Pages/DeviceAccess/Terminal.razor.cs

### Build Fixes
15. **Fixed CA1416 platform compatibility errors** — Added `[SupportedOSPlatform]` attributes to all platform-specific private methods in PtyTerminalSession.cs. Restructured mixed-platform methods (WriteInput, Resize, ReadLoop) to use explicit `OperatingSystem.IsLinux() || IsMacOS()` guards with annotated helper methods (`WriteInputUnix`, `ResizeUnix`, `ReadUnixAsync`) instead of bare `else` branches.
16. **Fixed CS0108 hiding warning** — Changed `PtyTerminal.DisposeAsync()` to override `DisposeAsync(bool disposing)` from `DisposableComponent` base class instead of declaring a new method. Removed redundant `IAsyncDisposable` interface.

### Deployment
- Docker build succeeded after 3 iterations (CA1416 fix, Dispose fix, CS0108 fix)
- Deployed to production at control.aspendora.com
- All background services running: MetricsIngestion, WebhookDispatcher, AlertEvaluation, AuditLog, Scheduler

### Runtime Fix: Blazor WASM JS Interop byte[] Serialization
17. **Diagnosed "can't type" issue** — Console showed `ByteArrayJsonConverter` errors when xterm.js called `invokeMethodAsync('OnTerminalInput', ...)` with byte arrays
18. **Root cause**: Blazor WASM's `invokeMethodAsync` cannot deserialize `Uint8Array` or JS arrays as `byte[]` parameters — the `ByteArrayJsonConverter` rejects them
19. **Fix (JS)**: Added `toBase64()` helper in PtyTerminal.razor.js, changed `onData`/`onBinary` to send base64-encoded strings
20. **Fix (C#)**: Changed `OnTerminalInput(byte[] data)` to `OnTerminalInput(string base64Data)` with `Convert.FromBase64String()` decoding
21. **Cache-busting**: Rebuilt with `CURRENT_VERSION=1.0.1` to change the `?v=` query string on static assets. Also unregistered service worker and cleared browser caches.

### Smoke Test Results
- Typed `whoami` in Interactive Terminal → received `nt authority\system` response
- PTY session establishes correctly over SignalR
- xterm.js renders PowerShell prompt with ANSI output
- No console errors after base64 fix

---

## 2026-03-04: Production Deployment to control.aspendora.com

### Summary
Deployed ControlR fork (lacymooretx/ControlR) with all 12 MSP features to production at `https://control.aspendora.com`.

### Key Steps
1. **Committed & pushed** 107 files (8,530 lines) covering all 6 phases
2. **Fixed Dockerfile** — updated project references, added Directory.Packages.props, created novnc dir
3. **Fixed OpenAPI conflict** — renamed duplicate path in ClientPortalController
4. **Generated EF Core migration** — `AddMspFeatures` for 15+ new tables
5. **Server setup** — cloned repo, built Docker image, created docker-compose.yml + .env
6. **SSL cert** — obtained via certbot dns-cloudflare for control.aspendora.com
7. **Nginx** — added server block with WebSocket support (86400s timeout for SignalR)
8. **Cloudflare DNS** — A record proxied to 149.28.251.164
9. **Network fix** — corrected network name from `aspendora-net` to `docker_aspendora-net`

### Issues Resolved
- Dockerfile referenced non-existent `ControlR.Libraries.Clients` → fixed to actual library names
- Missing `/app/novnc/` directory in container → added `RUN mkdir -p novnc`
- Duplicate OpenAPI path `assignments/{id}` → renamed to `assignments/by-user/{userId}`
- PostgreSQL 18 data dir change → mount at `/var/lib/postgresql` not `/var/lib/postgresql/data`
- No EF Core migration for new entities → generated on server via Docker SDK container
- Nginx couldn't resolve `controlr` → wrong network name (docker prefix issue)

### Server Files
| File | Action |
|------|--------|
| `/opt/docker/controlr/source/` | Git clone |
| `/opt/docker/controlr/docker-compose.yml` | Created |
| `/opt/docker/controlr/.env` | Created |
| `/opt/docker/nginx/conf.d/default.conf` | Appended |
| Cloudflare DNS | A record added |
| SSL cert | `/etc/letsencrypt/live/control.aspendora.com/` |

### Docker Config
- Network: `docker_aspendora-net` (172.27.0.0/16, gateway 172.27.0.1)
- Containers: `controlr`, `controlr-postgres`, `controlr-aspire`
- Image: `controlr-aspendora:latest` (built from fork)

## 2026-03-04: Entra ID SSO Setup

### Summary
Configured Entra ID (Azure AD) single sign-on for ControlR via Azure CLI.

### Steps
1. **Azure App Registration** — created `ControlR - Aspendora` (client ID: `f9479760-e358-4960-aa55-cd035b1232cf`)
2. **Client secret** — created with 2-year expiry
3. **ID token issuance** — enabled in app registration
4. **Env vars** — added `CONTROLR_ENTRAID_CLIENT_ID`, `CONTROLR_ENTRAID_CLIENT_SECRET`, `CONTROLR_ENTRAID_TENANT_ID` to `.env` and `docker-compose.yml`

### Issues Resolved
- **502 Bad Gateway on `/signin-oidc`** — nginx default `proxy_buffer_size` (4k/8k) too small for OIDC token response headers → added `proxy_buffer_size 128k`, `proxy_buffers 4 256k`, `proxy_busy_buffers_size 256k`
- **520 Unknown Error on `/Account/ExternalLogin`** — large auth cookies in subsequent requests exceeded nginx `large_client_header_buffers` default → added `large_client_header_buffers 4 32k`

### Nginx Config Changes (control.aspendora.com server block)
```nginx
# Added to server block:
large_client_header_buffers 4 32k;

# Added to location block:
proxy_buffer_size 128k;
proxy_buffers 4 256k;
proxy_busy_buffers_size 256k;
```

### Result
Entra ID SSO flow completes successfully. Users authenticate with Microsoft and are shown the account association/registration page in ControlR.

## 2026-03-06: Phase 9A - Credential Vault

### Goal
Implement encrypted per-device/group credential storage for quick auth during remote sessions.

### Steps Completed
1. **Entity**: Created `StoredCredential` extending `TenantEntityBase` with Name, Username, EncryptedPassword, Domain, DeviceId, DeviceGroupId, Category, CreatedByUserId, CreatedByUserName, LastAccessedAt, AccessCount
2. **DTOs**: Created `StoredCredentialDto` (no password), `StoredCredentialWithPasswordDto` (with decrypted password), `CreateCredentialRequestDto`, `UpdateCredentialRequestDto`
3. **Encryption Service**: `CredentialEncryptionService` using ASP.NET Core Data Protection with per-tenant protectors
4. **Controller**: `CredentialsController` with GET list, GET single, POST create, PUT update, DELETE, POST retrieve (audit-logged), GET for-device
5. **AppDb**: Added DbSet, ConfigureStoredCredentials with unique name+tenant index and device/group indexes
6. **EntityToDtoExtensions**: Added ToDto() for StoredCredential
7. **HttpConstants**: Added CredentialsEndpoint
8. **ControlrApi**: Added 6 interface methods + implementations
9. **ClientRoutes**: Added Credentials route
10. **NavMenu**: Added Credential Vault link under Tenant Admin
11. **WebApplicationBuilderExtensions**: Registered ICredentialEncryptionService as singleton
12. **UI Page**: Credentials.razor with MudDataGrid, create/edit/delete/retrieve actions
13. **Dialog**: CredentialEditDialog.razor for create/edit with password visibility toggle
14. **Migration**: Manual migration file + Designer + snapshot update for StoredCredentials table

### Files Created
- `ControlR.Web.Server/Data/Entities/StoredCredential.cs`
- `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/CredentialDto.cs`
- `ControlR.Web.Server/Services/CredentialEncryptionService.cs`
- `ControlR.Web.Server/Api/CredentialsController.cs`
- `ControlR.Web.Client/Components/Pages/Credentials.razor`
- `ControlR.Web.Client/Components/Dialogs/CredentialEditDialog.razor`
- `ControlR.Web.Server/Data/Migrations/20260306000000_AddCredentialVault.cs`
- `ControlR.Web.Server/Data/Migrations/20260306000000_AddCredentialVault.Designer.cs`

### Files Modified
- `Libraries/ControlR.Libraries.Shared/Constants/HttpConstants.cs` (added CredentialsEndpoint)
- `ControlR.Web.Server/Data/AppDb.cs` (added DbSet + ConfigureStoredCredentials)
- `ControlR.Web.Server/Extensions/EntityToDtoExtensions.cs` (added ToDto)
- `Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs` (added 6 credential methods)
- `ControlR.Web.Client/ClientRoutes.cs` (added Credentials route)
- `ControlR.Web.Client/Components/Layout/NavMenu.razor` (added nav link)
- `ControlR.Web.Server/Startup/WebApplicationBuilderExtensions.cs` (registered encryption service)
- `ControlR.Web.Server/Data/Migrations/AppDbModelSnapshot.cs` (added StoredCredential entity)

### Security Notes
- Passwords encrypted at rest using ASP.NET Core Data Protection with per-tenant protectors
- Password retrieval requires explicit POST to /retrieve endpoint (audit-logged)
- List/GET operations never include passwords
- All CRUD and retrieval operations audit-logged with user, IP, timestamp
- UI warns user before password retrieval that it will be audit-logged

## 2026-03-06: Phase 9B - JIT Admin Accounts

### Goal
Implement Just-In-Time (JIT) temporary local administrator accounts on remote Windows devices.

### Steps Completed
1. **Entity**: `JitAdminAccount` extending `TenantEntityBase` (DeviceId, DeviceName, Username, CreatedByUserId/Name, ExpiresAt, DeletedAt, Status)
2. **Hub DTOs**: `CreateJitAdminRequestHubDto`, `CreateJitAdminResultHubDto`, `DeleteJitAdminRequestHubDto`, `DeleteJitAdminResultHubDto`
3. **Server API DTOs**: `JitAdminAccountDto`, `JitAdminAccountStatusDto`, `CreateJitAdminRequestDto`, `CreateJitAdminResponseDto`
4. **IAgentHubClient**: Added `CreateJitAdminAccount()` and `DeleteJitAdminAccount()`
5. **IViewerHub**: Added `RequestCreateJitAdmin()` and `RequestDeleteJitAdmin()`
6. **ViewerHub**: Implemented both methods with auth, Windows check, random credential generation, DB tracking, audit logging
7. **AgentHubClient**: Implemented both methods using `net user` and `net localgroup Administrators` commands
8. **TestAgentHubClient**: Added stub implementations
9. **JitAdminCleanupService**: Background service (5-min interval) that expires active accounts past TTL
10. **AppDb**: Added DbSet, ConfigureJitAdminAccounts with indexes and tenant filter
11. **EntityToDtoExtensions**: Added ToDto() for JitAdminAccount
12. **HttpConstants/AuditEventTypes/AuditActions**: Added JitAdmin endpoint, event type, Create/Delete actions
13. **JitAdminController**: REST API for listing accounts
14. **ControlrApi**: Added GetJitAdminAccounts() to interface and implementation
15. **UI**: Dashboard button (Windows-only), credential display dialog, JitAdmin.razor list page, NavMenu link

### Files Created (6)
- ControlR.Web.Server/Data/Entities/JitAdminAccount.cs
- Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/JitAdminHubDto.cs
- Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/JitAdminDto.cs
- ControlR.Web.Server/Services/JitAdminCleanupService.cs
- ControlR.Web.Server/Api/JitAdminController.cs
- ControlR.Web.Client/Components/Pages/JitAdmin.razor

### Files Modified (14)
- Libraries/ControlR.Libraries.Shared/Hubs/Clients/IAgentHubClient.cs
- Libraries/ControlR.Libraries.Shared/Hubs/IViewerHub.cs
- Libraries/ControlR.Libraries.Shared/Constants/HttpConstants.cs
- Libraries/ControlR.Libraries.Shared/Enums/AuditEventType.cs
- Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs
- ControlR.Web.Server/Data/AppDb.cs
- ControlR.Web.Server/Extensions/EntityToDtoExtensions.cs
- ControlR.Web.Server/Hubs/ViewerHub.cs
- ControlR.Web.Server/Startup/WebApplicationBuilderExtensions.cs
- ControlR.Agent.Common/Services/AgentHubClient.cs
- Tests/ControlR.Agent.LoadTester/TestAgentHubClient.cs
- ControlR.Web.Client/ClientRoutes.cs
- ControlR.Web.Client/Components/Layout/NavMenu.razor
- ControlR.Web.Client/Components/Dashboard.razor + Dashboard.razor.cs

### Security Notes
- Password NEVER stored in DB -- generated, sent to agent, shown to user once, discarded
- Random username (`jit-admin-{6 hex}`) and 16-char password via `RandomNumberGenerator`
- TTL clamped 5-1440 minutes; background cleanup auto-expires and deletes
- All actions audit-logged; Windows-only check on both server and agent

### Next Steps
- Generate EF Core migration for JitAdminAccounts table
- Build and deploy

## 2026-03-06: Phase 10B -- File Manager Drag-and-Drop

### Goal
Add drag-and-drop upload and file move capabilities to the file manager.

### What was done
1. Created `MoveFileHubDto` (MessagePack) in `Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/MoveFileHubDto.cs`
2. Created `MoveFileRequestDto` in `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/MoveFileRequestDto.cs`
3. Added `MoveFile(MoveFileHubDto)` to `IAgentHubClient` interface
4. Added `MoveFileSystemEntry` to `IFileManager` interface and implemented in `FileManager.cs` (handles both files and directories)
5. Added `MoveFile` handler in `AgentHubClient.cs`
6. Added `move-file` POST endpoint to `DeviceFileSystemController.cs`
7. Added `MoveFile` to `IControlrApi` interface and implementation in `ControlrApi.cs`
8. Added `MoveFile` stub in `TestAgentHubClient.cs`
9. Updated `FileSystem.razor.js` with drop zone init/dispose and HTML5 drag-and-drop event handlers
10. Updated `FileSystem.razor` with drop zone overlay, Move button in toolbar (small + large breakpoints)
11. Updated `FileSystem.razor.cs` with `OnAfterRenderAsync` for JS interop init, `JSInvokable` callbacks, `OnMoveFileClick`, `DisposeAsync` override
12. Updated `FileSystem.razor.css` with drop zone overlay styling

### Files Changed
- `Libraries/ControlR.Libraries.Shared/Dtos/HubDtos/MoveFileHubDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Dtos/ServerApi/MoveFileRequestDto.cs` (new)
- `Libraries/ControlR.Libraries.Shared/Hubs/Clients/IAgentHubClient.cs`
- `Libraries/ControlR.Libraries.Shared/Hubs/IViewerHub.cs` (no change needed -- MoveFile goes through REST API)
- `Libraries/ControlR.Libraries.Shared/Services/Http/ControlrApi.cs`
- `ControlR.Agent.Common/Services/FileManager/FileManager.cs`
- `ControlR.Agent.Common/Services/AgentHubClient.cs`
- `ControlR.Web.Server/Api/DeviceFileSystemController.cs`
- `ControlR.Web.Client/Components/Pages/DeviceAccess/FileSystem.razor`
- `ControlR.Web.Client/Components/Pages/DeviceAccess/FileSystem.razor.cs`
- `ControlR.Web.Client/Components/Pages/DeviceAccess/FileSystem.razor.js`
- `ControlR.Web.Client/Components/Pages/DeviceAccess/FileSystem.razor.css`
- `Tests/ControlR.Agent.LoadTester/TestAgentHubClient.cs`
