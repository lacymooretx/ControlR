namespace ControlR.Libraries.Shared.Enums;

public static class AuditEventTypes
{
  public const string AgentUpdate = "AgentUpdate";
  public const string Chat = "Chat";
  public const string FileTransfer = "FileTransfer";
  public const string Login = "Login";
  public const string LoginFailed = "LoginFailed";
  public const string Logout = "Logout";
  public const string PowerState = "PowerState";
  public const string RemoteControl = "RemoteControl";
  public const string ScriptExecution = "ScriptExecution";
  public const string Terminal = "Terminal";
  public const string UninstallAgent = "UninstallAgent";
  public const string WakeDevice = "WakeDevice";
}

public static class AuditActions
{
  public const string Completed = "Completed";
  public const string Download = "Download";
  public const string End = "End";
  public const string Execute = "Execute";
  public const string Failed = "Failed";
  public const string Invoke = "Invoke";
  public const string Restart = "Restart";
  public const string Send = "Send";
  public const string Shutdown = "Shutdown";
  public const string Start = "Start";
  public const string Success = "Success";
  public const string Trigger = "Trigger";
  public const string Upload = "Upload";
}
