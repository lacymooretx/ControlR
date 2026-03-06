# Phase 7 Architecture Summary

Concise reference for implementing on-demand sessions, session recording, and backstage mode.

---

## A. On-Demand Sessions

### Agent Registration/Authentication

- **AgentHub is UNAUTHENTICATED** -- no `[Authorize]` attribute. Any WebSocket client can connect to `/hubs/agent`.
- Agent connects via `HubConnectionInitializer.Connect()` in `ControlR.Agent.Common/Services/HubConnectionInitializer.cs`
  - Endpoint: `{ServerUri}/hubs/agent` (constant: `AppConstants.AgentHubPath`)
  - Transport: WebSockets only (`SkipNegotiation = true`)
- Agent self-identifies by calling `AgentHub.UpdateDevice(DeviceUpdateRequestDto)` on first connect and every 5min heartbeat (`AgentHeartbeatTimer`)
- `UpdateDevice` creates or updates the `Device` entity in the DB, stores `Context.ConnectionId`, and adds the agent to SignalR groups (tenant, device, tags)
- Agent config: `ISettingsProvider` provides `DeviceId` (Guid), `TenantId` (Guid), `ServerUri`, `InstanceId`
  - Config stored in `appsettings.json` on the agent machine
  - If `AllowAgentsToSelfBootstrap` is true, agents with `TenantId=Guid.Empty` get auto-assigned to the last tenant

### Remote Session Initiation Flow

```
Viewer (Blazor WASM)
  --> ViewerHub.RequestRemoteControlSession(deviceId, sessionRequestDto)
    --> TryAuthorizeAgainstDevice(deviceId)  [checks user auth + DeviceAccessByDeviceResourcePolicy]
    --> Reads TenantSetting "NotifyUserOnSessionStart"
    --> Sets ViewerName, ViewerConnectionId on the DTO
    --> agentHub.Clients.Client(device.ConnectionId).CreateRemoteControlSession(dto)

Agent (AgentHubClient.CreateRemoteControlSession)
  --> Ensures DesktopClient is latest version
  --> Finds IPC server for target process (TargetProcessId = desktop session PID)
  --> Forwards as RemoteControlRequestIpcDto to DesktopClient via IPC

DesktopClient (RemoteControlHostManager.StartHost)
  --> Creates a new IHost for the session
  --> Configures RemoteControlSessionOptions (SessionId, WebSocketUri, NotifyUser, etc.)
  --> Starts RemoteControlSessionInitializer (BackgroundService)
    --> DesktopRemoteControlStream.StreamScreen()
      --> Optionally requests user consent
      --> Connects to WebSocket relay at sessionRequestDto.WebsocketUri
      --> Starts screen capture loop
```

### Key DTO: `RemoteControlSessionRequestDto`
- File: `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/RemoteControlSessionRequestDto.cs`
- Fields: `SessionId`, `WebsocketUri`, `TargetSystemSession`, `TargetProcessId`, `DeviceId`, `NotifyUserOnSessionStart`, `RequireConsent`, `ViewerConnectionId`, `ViewerName`

### What a Temp Agent Would Need

- A way to generate a short-lived access token/link (the `LogonTokenProvider` pattern already exists -- see below)
- A lightweight agent binary that can connect to the AgentHub, call `UpdateDevice`, and respond to `CreateRemoteControlSession`
- The AgentHub already handles self-bootstrapping with `AllowAgentsToSelfBootstrap`
- No auth on AgentHub means any agent can connect -- **tenant scoping** is the main concern

### Existing Token Infrastructure: LogonTokenProvider

Already implemented for device-scoped auth:

- `ControlR.Web.Server/Services/LogonTokens/LogonTokenProvider.cs` -- creates one-time-use tokens stored in `IMemoryCache`, scoped to a `(deviceId, tenantId, userId)`, expires in N minutes
- `ControlR.Web.Server/Authn/LogonTokenAuthenticationHandler.cs` -- validates `?logonToken=X&deviceId=Y` query params, creates claims principal with `DeviceSessionScope` claim
- Tokens are single-use (marked consumed after validation)
- This pattern can be extended for temp agent sessions

### DB Session Entities

- **No dedicated "session" entity exists in the DB.**
- The `AuditLog` entity tracks session events:
  - `AuditLog.SessionId` (nullable Guid) -- set when auditing terminal/remote control sessions
  - `AuditLog.EventType` + `AuditLog.Action` -- e.g., `("RemoteControl", "Start")`, `("RemoteControl", "End")`, `("Terminal", "Start"/"End")`
  - Also tracks: `ActorUserId`, `ActorUserName`, `TargetDeviceId`, `TargetDeviceName`, `SourceIpAddress`, `Timestamp`
- File: `ControlR.Web.Server/Data/Entities/AuditLog.cs`

---

## B. Session Recording

### Frame Format and Encoding

Two encoder paths exist:

1. **JPEG (default, production-ready)** -- `FrameBasedCapturer` + `JpegEncoder`
   - Captures screen via `IScreenGrabber` (platform-specific: DXGI on Windows, CGWindow on Mac, Wayland/X11 on Linux)
   - Returns `SKBitmap` (SkiaSharp)
   - Dirty-rect detection: only encodes changed regions
   - Encodes changed region as JPEG via `JpegEncoder.EncodeRegion(bitmap, region, quality)`
   - Wraps in `ScreenRegionDto(X, Y, Width, Height, EncodedImage)` with `DtoType.ScreenRegion`
   - Quality is configurable via `RemoteControlSessionState.ImageQuality`

2. **VP9 (stub, not production-ready)** -- `StreamBasedCapturer` + `Vp9Encoder`
   - Pipes raw BGRA frames to ffmpeg's `libvpx-vp9`
   - Outputs WebM/VP9 packets as `VideoStreamPacketDto(PacketData, Timestamp)` with `DtoType.VideoStreamPacket`
   - Target framerate: 30fps

### Frame Flow: Agent -> Server -> Viewer

```
Agent DesktopClient
  IScreenGrabber.CaptureDisplay() -> SKBitmap
  FrameBasedCapturer: dirty-rect detection, JPEG encoding
  -> ScreenRegionDto wrapped in DtoWrapper
  -> DesktopRemoteControlStream.Send(wrapper)  [extends ManagedRelayStream]
  -> ClientWebSocket -> WebSocket Relay Server

WebSocket Relay (separate server process or embedded middleware)
  WebSocketRelayMiddleware: pairs two WebSocket connections (requester + responder)
  - Both connect with ?sessionId=X&accessToken=Y&role=requester|responder
  - Raw binary relay: reads from one socket, writes to the other
  - No frame inspection or modification server-side
  - 256KB buffer

Viewer (Blazor WASM)
  ManagedRelayStream.ReadFromStream() -> deserialize DtoWrapper (MessagePack)
  -> ScreenRegionDto: draw JPEG region onto HTML5 canvas at (X, Y, Width, Height)
```

**Key insight for recording:** The server currently acts as a **dumb WebSocket relay** -- it never sees the frame data. To record sessions, you would need to either:
1. Tap the relay stream server-side (modify `WebSocketRelayMiddleware.StreamToPartner()` to tee frames)
2. Have the agent send a parallel recording stream to a server endpoint
3. Record client-side in the viewer

### Frame Rate / Frequency

- JPEG mode: adaptive, driven by dirty-rect changes. No fixed FPS. Metrics reported every 3 seconds via `CaptureMetricsDto`
- VP9 mode: targets 30fps (but is a stub)
- Capture metrics include: `Fps`, `CaptureMode` (e.g., "DirectX", "GDI")

### File Storage Infrastructure

- **No file/blob storage exists on the server.**
- File transfers use streaming channels (viewer -> hub -> agent or vice versa) with no server-side persistence
- `AppOptions.MaxFileTransferSize` limits upload size
- Any recording storage would need to be built from scratch

### Existing Audit Data

- `AuditService` (`ControlR.Web.Server/Services/AuditService.cs`) -- fire-and-forget channel-based logger
- `AuditLogBackgroundService` -- batches writes to PostgreSQL
- Webhooks dispatched for: `session.remote_control.start`, `session.remote_control.end`, `session.terminal.start`, `session.terminal.end`, `file.uploaded`, `file.downloaded`
- Audit entries include `SessionId` but no duration tracking (no `EndTimestamp` populated currently)

### DTO Serialization

- Remote control DTOs use **MessagePack** for WebSocket relay (binary, fast)
- Hub DTOs use **JSON** for SignalR communication
- `DtoWrapper` wraps all remote control payloads with a `DtoType` enum discriminator

---

## C. Backstage / Privacy Mode

### Existing Privacy Screen Code

- **Windows only.** DTOs exist for all platforms but only Windows implements it.
- Toggle flow:
  1. Viewer sends `DtoType.TogglePrivacyScreen` -> `TogglePrivacyScreenDto(IsEnabled)` via WebSocket relay
  2. `DesktopRemoteControlStream.HandleMessageReceived()` handles it (line ~317 in `DesktopRemoteControlStream.cs`)
  3. Calls `IDisplayManager.SetPrivacyScreen(isEnabled)`
  4. Sends back `PrivacyScreenResultDto(IsSuccess, FinalState)` via `DtoType.PrivacyScreenResult`

- Windows implementation (`DisplayManagerWindows.SetPrivacyScreen()`):
  - Creates a topmost, transparent, layered popup window covering all virtual screens
  - Uses `Win32Interop.CreatePrivacyScreenWindow()` with `WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED` styles
  - Tracked via `_privacyWindow` handle; `IsPrivacyScreenEnabled => _privacyWindow != nint.Zero`
  - `DestroyPrivacyScreenWindow()` tears it down

- **Current limitation:** This is a visual overlay only. It does NOT blank the physical display or disable user input independently. The user can still see through it (WS_EX_TRANSPARENT). It's more of a "curtain" than true screen blanking.

### NotifyUserOnSessionStart

- Tenant-level setting stored in `TenantSettings` table (key: `"NotifyUserOnSessionStart"`)
- Read in `ViewerHub.RequestRemoteControlSession()` and injected into the session DTO
- Propagated to `RemoteControlSessionOptions.NotifyUser`
- In `DesktopRemoteControlStream.StreamScreen()`:
  - If `NotifyUser` is true: shows a toast notification via `IToaster` saying "Admin X started a remote control session"
  - Also shows a toast when the session ends
- `RequireConsent`: if true, `ISessionConsentService.RequestConsentAsync()` is called, which shows a dialog and waits for user approval. If denied, the session is aborted.

### Windows Service Mode (Agent)

- Agent runs as a Windows service (`ControlR.Agent`) in Session 0
- `DesktopClientWatcherWin` (BackgroundService) monitors all active desktop sessions
- Every 5 seconds, enumerates active Windows sessions via `Win32Interop.GetActiveSessions()`
- For each session without a DesktopClient, launches one via `Win32Interop.CreateInteractiveSystemProcess()` (creates process in the target user's session)
- The DesktopClient runs in each user's interactive session, connects to the Agent via IPC (named pipes), and handles screen capture + remote control
- IPC architecture: `IpcServerStore` maps process IDs to IPC servers; `AgentHubClient` routes hub calls to the right DesktopClient via `ipcServer.Server.Client.*`

### Block Input (Related Feature)

- `DtoType.ToggleBlockInput` -> `ToggleBlockInputDto(IsEnabled)`
- Windows only: calls `IInputSimulator.SetBlockInput()` which uses Win32 `BlockInput()` API
- Sends back `BlockInputResultDto(IsSuccess, FinalState)`
- Currently independent of privacy screen

### What Full Backstage Mode Would Require

1. **True screen blanking** (not just overlay):
   - Windows: Turn off monitors via `SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2)` or use DXGI output duplication to show black
   - Mac: `IOPMAssertionCreateWithName` or CGDisplayCapture
   - Linux: `xrandr --output X --brightness 0` or DPMS
2. **Input blocking** (already exists for Windows via `BlockInput()`)
3. **Desktop switching** (Windows): switch to a hidden desktop to prevent screen observation
4. **Coordinated toggle**: combine privacy screen + input block into a single "backstage mode" command
5. **Auto-restore on disconnect**: ensure screens and input are restored if the session drops
6. **Platform support**: Mac and Linux `IDisplayManager.SetPrivacyScreen()` currently return `Result.Fail` (not implemented)

---

## Key File Paths Reference

| Component | Path |
|-----------|------|
| AgentHub | `ControlR.Web.Server/Hubs/AgentHub.cs` |
| ViewerHub | `ControlR.Web.Server/Hubs/ViewerHub.cs` |
| IAgentHub interface | `Libraries/ControlR.Libraries.Shared/Hubs/IAgentHub.cs` |
| IViewerHub interface | `Libraries/ControlR.Libraries.Shared/Hubs/IViewerHub.cs` |
| IAgentHubClient interface | `Libraries/ControlR.Libraries.Shared/Hubs/Clients/IAgentHubClient.cs` |
| Agent hub connection | `ControlR.Agent.Common/Services/HubConnectionInitializer.cs` |
| Agent heartbeat | `ControlR.Agent.Common/Services/AgentHeartbeatTimer.cs` |
| Agent hub client | `ControlR.Agent.Common/Services/AgentHubClient.cs` |
| Agent settings | `ControlR.Agent.Common/Services/SettingsProvider.cs` |
| Agent installer CLI | `ControlR.Agent/Startup/CommandProvider.cs` |
| Desktop client watcher (Win) | `ControlR.Agent.Common/Services/Windows/DesktopClientWatcherWin.cs` |
| Remote control stream | `ControlR.DesktopClient.Common/Services/DesktopRemoteControlStream.cs` |
| Remote control host mgr | `ControlR.DesktopClient/Services/RemoteControlHostManager.cs` |
| Session initializer | `ControlR.DesktopClient.Common/Services/RemoteControlSessionInitializer.cs` |
| Session options | `ControlR.DesktopClient.Common/Options/RemoteControlSessionOptions.cs` |
| Frame-based capturer | `ControlR.DesktopClient.Common/Services/FrameBasedCapturer.cs` |
| Desktop capturer factory | `ControlR.DesktopClient.Common/Services/DesktopCapturerFactory.cs` |
| IScreenGrabber | `ControlR.DesktopClient.Common/ServiceInterfaces/IScreenGrabber.cs` |
| IDisplayManager | `ControlR.DesktopClient.Common/ServiceInterfaces/IDisplayManager.cs` |
| DisplayManagerWindows | `ControlR.DesktopClient.Windows/Services/DisplayManagerWindows.cs` |
| IStreamEncoder | `ControlR.DesktopClient.Common/Services/Encoders/IStreamEncoder.cs` |
| JpegEncoder | `ControlR.DesktopClient.Common/Services/Encoders/JpegEncoder.cs` |
| Vp9Encoder (stub) | `ControlR.DesktopClient.Common/Services/Encoders/Vp9Encoder.cs` |
| WebSocket relay middleware | `Libraries/ControlR.Libraries.WebSocketRelay.Common/Middleware/WebSocketRelayMiddleware.cs` |
| ManagedRelayStream | `Libraries/ControlR.Libraries.WebSocketRelay.Client/ManagedRelayStream.cs` |
| Remote control DTOs | `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/` |
| DtoType enum | `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/DtoType.cs` |
| ScreenRegionDto | `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/ScreenRegionDto.cs` |
| VideoStreamPacketDto | `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/VideoStreamPacketDto.cs` |
| Privacy screen DTOs | `Libraries/ControlR.Libraries.Shared/Dtos/RemoteControlDtos/TogglePrivacyScreenDto.cs` |
| Win32 interop | `Libraries/ControlR.Libraries.NativeInterop.Windows/Win32Interop.cs` |
| LogonTokenProvider | `ControlR.Web.Server/Services/LogonTokens/LogonTokenProvider.cs` |
| LogonToken auth handler | `ControlR.Web.Server/Authn/LogonTokenAuthenticationHandler.cs` |
| AuditService | `ControlR.Web.Server/Services/AuditService.cs` |
| AuditLog entity | `ControlR.Web.Server/Data/Entities/AuditLog.cs` |
| Device entity | `ControlR.Web.Server/Data/Entities/Device.cs` |
| App constants | `Libraries/ControlR.Libraries.Shared/Constants/AppConstants.cs` |
