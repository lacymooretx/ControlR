namespace ControlR.Web.Client.Services;

public interface IBrandingState
{
  string ProductName { get; }
  string PrimaryColor { get; }
  string? SecondaryColor { get; }
  string? LogoUrl { get; }
  bool IsLoaded { get; }
  Task LoadAsync();
  Task RefreshAsync();
}
