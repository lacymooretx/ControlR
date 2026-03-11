using System.Threading.Channels;
using ControlR.Libraries.Shared.Dtos.Devices;
using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Dtos.HubDtos.PwshCommandCompletions;
using ControlR.Libraries.Shared.Dtos.RemoteControlDtos;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using ControlR.Libraries.Shared.Enums;

namespace ControlR.Libraries.Shared.Hubs;

public interface IViewerHub
{
  Task<Result> CloseChatSession(Guid deviceId, Guid sessionId, int targetProcessId);
  Task ClosePtySession(Guid deviceId, Guid terminalSessionId);
  Task CloseTerminalSession(Guid deviceId, Guid terminalSessionId);

  Task<Result> CreatePtySession(Guid deviceId, Guid terminalSessionId, int cols, int rows);

  Task<Result<CreateJitAdminResponseDto>> RequestCreateJitAdmin(Guid deviceId, int ttlMinutes = 60);
  Task<Result> RequestDeleteJitAdmin(Guid deviceId, Guid jitAccountId);

  Task<Result> CreateTerminalSession(
    Guid deviceId,
    Guid terminalSessionId);

  Task<Result<ScriptExecutionDto>> ExecuteScript(ExecuteScriptRequestDto request);
  Task<DesktopSession[]> GetActiveDesktopSessions(Guid deviceId);
  Task<Result<PwshCompletionsResponseDto>> GetPwshCompletions(PwshCompletionsRequestDto request);
  Task<Result> InvokeCtrlAltDel(Guid deviceId, int targetDesktopProcessId, DesktopSessionType desktopSessionType);
  Task RefreshDeviceInfo(Guid deviceId);

  Task<Result> RequestRemoteControlSession(Guid deviceId, RemoteControlSessionRequestDto sessionRequestDto);
  Task<Result> RequestVncSession(Guid deviceId, VncSessionRequestDto sessionRequestDto);
  Task SendAgentUpdateTrigger(Guid deviceId);
  Task<Result> SendChatMessage(Guid deviceId, ChatMessageHubDto dto);
  Task SendDtoToAgent(Guid deviceId, DtoWrapper wrapper);
  Task SendDtoToUserGroups(DtoWrapper wrapper);
  Task SendPowerStateChange(Guid deviceId, PowerStateChangeType changeType);
  Task<Result> RequestSafeModeReboot(Guid deviceId, bool withNetworking = true);
  Task<Result> SendPtyInput(Guid deviceId, PtyInputDto dto);
  Task<Result> SendTerminalInput(Guid deviceId, TerminalInputDto dto);
  Task SendWakeDevice(Guid deviceId, string[] macAddresses);
  Task<Result> ResizePty(Guid deviceId, PtyResizeDto dto);
  Task<Result> TestVncConnection(Guid guid, int port);
  Task UninstallAgent(Guid deviceId, string reason);
  Task<Result> RequestPatchScan(Guid deviceId);
  Task<Result> RequestPatchInstall(Guid deviceId, string[] updateIds);
  Task<Result> UploadFile(FileUploadMetadata metadata, ChannelReader<byte[]> fileStream);

  Task<Result> StartStandaloneWebcam(Guid deviceId, int cameraIndex, int preferredWidth, int preferredHeight);
  Task<Result> StopStandaloneWebcam(Guid deviceId);
  Task<Result> GetStandaloneWebcamList(Guid deviceId);
}