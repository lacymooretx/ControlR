namespace ControlR.Agent.Common.Services.Terminal;

public interface ITerminalSessionFactory
{
  Task<Result<ITerminalSession>> CreateSession(Guid terminalId, string viewerConnectionId);
  Task<Result<IPtyTerminalSession>> CreatePtySession(Guid terminalId, string viewerConnectionId, int cols, int rows);
}

internal class TerminalSessionFactory(
  ISystemEnvironment systemEnvironment,
  TimeProvider timeProvider,
  IHubConnection<IAgentHub> hubConnection,
  ILogger<TerminalSession> sessionLogger,
  ILogger<PtyTerminalSession> ptySessionLogger,
  ILogger<TerminalSessionFactory> logger) : ITerminalSessionFactory
{
  public async Task<Result<ITerminalSession>> CreateSession(Guid terminalId, string viewerConnectionId)
  {
    try
    {
      var terminalSession = new TerminalSession(
        terminalId,
        viewerConnectionId,
        timeProvider,
        systemEnvironment,
        hubConnection,
        sessionLogger);

      await terminalSession.Initialize();

      logger.LogInformation("Terminal session created successfully. ID: {TerminalId}, Viewer: {ViewerConnectionId}",
        terminalId, viewerConnectionId);

      return Result.Ok<ITerminalSession>(terminalSession);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error while creating terminal session. ID: {TerminalId}, Viewer: {ViewerConnectionId}",
        terminalId, viewerConnectionId);
      return Result.Fail<ITerminalSession>("Failed to create terminal session.");
    }
  }

  public async Task<Result<IPtyTerminalSession>> CreatePtySession(Guid terminalId, string viewerConnectionId, int cols, int rows)
  {
    try
    {
      var ptySession = new PtyTerminalSession(
        terminalId,
        viewerConnectionId,
        hubConnection,
        ptySessionLogger);

      await ptySession.Initialize(cols, rows);

      logger.LogInformation("PTY session created successfully. ID: {TerminalId}, Viewer: {ViewerConnectionId}, Size: {Cols}x{Rows}",
        terminalId, viewerConnectionId, cols, rows);

      return Result.Ok<IPtyTerminalSession>(ptySession);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error while creating PTY session. ID: {TerminalId}, Viewer: {ViewerConnectionId}",
        terminalId, viewerConnectionId);
      return Result.Fail<IPtyTerminalSession>("Failed to create PTY session.");
    }
  }
}
