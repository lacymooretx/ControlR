using System.Threading.Channels;
using ControlR.Libraries.Shared.Dtos.HubDtos;
using ControlR.Libraries.Shared.Dtos.ServerApi;

namespace ControlR.Libraries.Shared.Hubs;

public interface IAgentHub
{
  ChannelReader<byte[]> GetFileStreamFromViewer(FileUploadHubDto dto);
  Task ReportInventory(InventoryReportHubDto report);
  Task ReportScriptResult(ScriptExecutionResultHubDto result);
  Task<bool> SendChatResponse(ChatResponseHubDto responseDto);
  Task SendDesktopPreviewStream(Guid streamId, ChannelReader<byte[]> jpegChunks);
  Task SendDirectoryContentsStream(Guid streamId, bool directoryExists, ChannelReader<FileSystemEntryDto[]> entryChunks);
  Task<Result> SendFileContentStream(Guid streamId, ChannelReader<byte[]> fileChunks);
  Task SendSubdirectoriesStream(Guid streamId, ChannelReader<FileSystemEntryDto[]> subdirectoryChunks);
  Task SendPtyOutputToViewer(string viewerConnectionId, PtyOutputDto outputDto);
  Task SendTerminalOutputToViewer(string viewerConnectionId, TerminalOutputDto outputDto);
  Task ReportPatchScanResult(PatchScanResultHubDto result);
  Task ReportPatchInstallResult(PatchInstallResultHubDto result);
  Task<Result<DeviceResponseDto>> UpdateDevice(DeviceUpdateRequestDto agentDto);

  Task SendStandaloneWebcamFrame(string viewerConnectionId, StandaloneWebcamFrameDto frame);
  Task SendStandaloneWebcamList(string viewerConnectionId, WebcamInfoDto[] cameras);
}
