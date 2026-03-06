using ControlR.Web.Client.Models;
using ControlR.Web.Client.StateManagement;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ControlR.Web.Client.Components.Layout;

public abstract class BaseLayout : LayoutComponentBase, IAsyncDisposable
{
  private MudTheme? _customTheme;

  [Inject]
  public required PersistentComponentState ApplicationState { get; set; }
  [Inject]
  public required AuthenticationStateProvider AuthState { get; set; }
  [Inject]
  public required ILogger<BaseLayout> BaseLogger { get; set; }
  [Inject]
  public required IBrandingState BrandingState { get; set; }
  [Inject]
  public required ILazyInjector<IJsInterop> JsInterop { get; set; }
  [Inject]
  public required ILazyInjector<IMessenger> Messenger { get; set; }
  [Inject]
  public required NavigationManager NavManager { get; set; }
  [Inject]
  public required ILazyInjector<ISnackbar> Snackbar { get; set; }
  [Inject]
  public required IUserSettingsProvider UserSettings { get; set; }

  protected Palette CurrentPalette => IsDarkMode
      ? CustomTheme.PaletteDark
      : CustomTheme.PaletteLight;
  protected ThemeMode CurrentThemeMode { get; set; } = ThemeMode.Auto;
  protected MudTheme CustomTheme
  {
    get
    {
      if (_customTheme is null || _brandingApplied != BrandingState.IsLoaded)
      {
        _customTheme = BuildTheme();
        _brandingApplied = BrandingState.IsLoaded;
      }
      return _customTheme;
    }
  }

  private bool _brandingApplied;

  private MudTheme BuildTheme()
  {
    var darkPalette = new PaletteDark
    {
      Primary = BrandingState.IsLoaded ? BrandingState.PrimaryColor : Theme.DarkPalette.Primary,
      Secondary = BrandingState.IsLoaded && !string.IsNullOrEmpty(BrandingState.SecondaryColor)
        ? BrandingState.SecondaryColor
        : Theme.DarkPalette.Secondary,
      Tertiary = Theme.DarkPalette.Tertiary,
      Info = Theme.DarkPalette.Info,
      Success = Theme.DarkPalette.Success,
      Warning = Theme.DarkPalette.Warning,
      Error = Theme.DarkPalette.Error,
      Dark = Theme.DarkPalette.Dark,
      TextPrimary = Theme.DarkPalette.TextPrimary,
      TextSecondary = Theme.DarkPalette.TextSecondary,
      TextDisabled = Theme.DarkPalette.TextDisabled,
      ActionDefault = Theme.DarkPalette.ActionDefault,
      ActionDisabled = Theme.DarkPalette.ActionDisabled,
      Background = Theme.DarkPalette.Background,
      BackgroundGray = Theme.DarkPalette.BackgroundGray,
      Surface = Theme.DarkPalette.Surface,
      AppbarBackground = Theme.DarkPalette.AppbarBackground,
      AppbarText = Theme.DarkPalette.AppbarText,
      DrawerBackground = Theme.DarkPalette.DrawerBackground,
      DrawerText = Theme.DarkPalette.DrawerText,
      Divider = Theme.DarkPalette.Divider,
      DividerLight = Theme.DarkPalette.DividerLight,
      TableLines = Theme.DarkPalette.TableLines,
      TableStriped = Theme.DarkPalette.TableStriped,
      TableHover = Theme.DarkPalette.TableHover,
      LinesDefault = Theme.DarkPalette.LinesDefault,
      LinesInputs = Theme.DarkPalette.LinesInputs,
      OverlayDark = Theme.DarkPalette.OverlayDark,
      OverlayLight = Theme.DarkPalette.OverlayLight
    };

    var lightPalette = new PaletteLight
    {
      Primary = BrandingState.IsLoaded ? BrandingState.PrimaryColor : Theme.LightPalette.Primary,
      Secondary = BrandingState.IsLoaded && !string.IsNullOrEmpty(BrandingState.SecondaryColor)
        ? BrandingState.SecondaryColor
        : Theme.LightPalette.Secondary,
      Tertiary = Theme.LightPalette.Tertiary,
      Info = Theme.LightPalette.Info,
      Success = Theme.LightPalette.Success,
      Warning = Theme.LightPalette.Warning,
      Error = Theme.LightPalette.Error,
      Dark = Theme.LightPalette.Dark,
      TextPrimary = Theme.LightPalette.TextPrimary,
      TextSecondary = Theme.LightPalette.TextSecondary,
      TextDisabled = Theme.LightPalette.TextDisabled,
      ActionDefault = Theme.LightPalette.ActionDefault,
      ActionDisabled = Theme.LightPalette.ActionDisabled,
      Background = Theme.LightPalette.Background,
      BackgroundGray = Theme.LightPalette.BackgroundGray,
      Surface = Theme.LightPalette.Surface,
      AppbarBackground = Theme.LightPalette.AppbarBackground,
      AppbarText = Theme.LightPalette.AppbarText,
      DrawerBackground = Theme.LightPalette.DrawerBackground,
      DrawerText = Theme.LightPalette.DrawerText,
      Divider = Theme.LightPalette.Divider,
      DividerLight = Theme.LightPalette.DividerLight,
      TableLines = Theme.LightPalette.TableLines,
      TableStriped = Theme.LightPalette.TableStriped,
      TableHover = Theme.LightPalette.TableHover,
      LinesDefault = Theme.LightPalette.LinesDefault,
      LinesInputs = Theme.LightPalette.LinesInputs,
      OverlayDark = Theme.LightPalette.OverlayDark,
      OverlayLight = Theme.LightPalette.OverlayLight
    };

    return new MudTheme
    {
      PaletteDark = darkPalette,
      PaletteLight = lightPalette
    };
  }
  protected bool DrawerOpen { get; set; } = true;
  protected bool IsAuthenticated { get; set; }
  protected bool IsDarkMode { get; set; } = true;
  protected PersistingComponentStateSubscription PersistingSubscription { get; set; }
  protected string ThemeClass => IsDarkMode ? "dark-mode" : "light-mode";

  public virtual ValueTask DisposeAsync()
  {
    try
    {
      PersistingSubscription.Dispose();
      if (RendererInfo.IsInteractive)
      {
        Messenger.Value.UnregisterAll(this);
      }
    }
    catch (Exception ex)
    {
      BaseLogger.LogError(ex, "Error during BaseLayout disposal.");
    }

    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }

  protected async Task<bool> GetSystemDarkMode()
  {
    try
    {
      if (RendererInfo.IsInteractive)
      {
        return await JsInterop.Value.GetSystemDarkMode();
      }
      return true; // Default to dark during prerendering
    }
    catch (Exception ex)
    {
      BaseLogger.LogWarning(ex, "Failed to get system dark mode preference. Defaulting to dark.");
      return true;
    }
  }
  protected virtual async Task HandleThemeChanged(ThemeMode mode)
  {
    CurrentThemeMode = mode;
    await UpdateIsDarkMode();
    StateHasChanged();
  }
  protected override async Task OnInitializedAsync()
  {
    await base.OnInitializedAsync();

    await BrandingState.LoadAsync();

    var authState = await AuthState.GetAuthenticationStateAsync();
    IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

    // Try to restore persisted state from SSR
    if (!ApplicationState.TryTakeFromJson<bool>(PersistentStateKeys.IsDarkMode, out var persistedIsDarkMode))
    {
      // No persisted state, this is SSR or first load
      if (IsAuthenticated)
      {
        CurrentThemeMode = await UserSettings.GetThemeMode();
      }
      await UpdateIsDarkMode();

      // Register a callback to persist state before SSR completes
      PersistingSubscription = ApplicationState.RegisterOnPersisting(PersistThemeState);
    }
    else
    {
      // Restored from persisted state (this is WASM after SSR)
      IsDarkMode = persistedIsDarkMode;

      // Still need to load theme mode
      if (IsAuthenticated)
      {
        CurrentThemeMode = await UserSettings.GetThemeMode();
      }
    }

    if (RendererInfo.IsInteractive)
    {
      Messenger.Value.Register<ToastMessage>(this, HandleToastMessage);
      Messenger.Value.Register<ThemeChangedMessage>(this, HandleThemeChangedMessage);
    }
  }
  protected Task PersistThemeState()
  {
    ApplicationState.PersistAsJson(PersistentStateKeys.IsDarkMode, IsDarkMode);
    return Task.CompletedTask;
  }
  protected void ToggleNavDrawer()
  {
    DrawerOpen = !DrawerOpen;
  }
  protected virtual async Task UpdateIsDarkMode()
  {
    IsDarkMode = CurrentThemeMode switch
    {
      ThemeMode.Light => false,
      ThemeMode.Dark => true,
      ThemeMode.Auto => await GetSystemDarkMode(),
      _ => true
    };
  }

  private async Task HandleThemeChangedMessage(object subscriber, ThemeChangedMessage message)
  {
    await HandleThemeChanged(message.ThemeMode);
  }
  private Task HandleToastMessage(object subscriber, ToastMessage toast)
  {
    Snackbar.Value.Add(toast.Message, toast.Severity);
    return Task.CompletedTask;
  }
}
