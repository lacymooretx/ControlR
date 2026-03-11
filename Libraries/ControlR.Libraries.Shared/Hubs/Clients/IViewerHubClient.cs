using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using ControlR.Libraries.Shared.Dtos.Ui;

namespace ControlR.Libraries.Shared.Hubs.Clients;

public interface IViewerHubClient
{
  Task InvokeToast(ToastInfo toastInfo);
  Task<bool> ReceiveChatResponse(ChatResponseHubDto dto);
  Task ReceiveDeviceUpdate(DeviceResponseDto deviceDto);
  Task ReceiveScriptExecutionProgress(ScriptExecutionResultHubDto result);
  Task ReceivePatchScanProgress(PatchScanResultHubDto result);
  Task ReceivePatchInstallProgress(PatchInstallResultHubDto result);
  Task ReceiveServerStats(ServerStatsDto serverStats);
  Task ReceivePtyOutput(PtyOutputDto output);
  Task ReceiveTerminalOutput(TerminalOutputDto output);

  Task ReceiveStandaloneWebcamFrame(StandaloneWebcamFrameDto frame);
  Task ReceiveStandaloneWebcamList(WebcamInfoDto[] cameras);
}