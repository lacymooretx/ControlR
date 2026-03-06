namespace ControlR.Web.Client.Components.RemoteDisplays;

public partial class ConnectionQualityIndicator : IDisposable
{
  private IDisposable? _stateChangedRegistration;
  private ConnectionQuality _quality = ConnectionQuality.Good;
  private Color _iconColor = Color.Success;

  [Inject]
  public required IMetricsState MetricsState { get; init; }

  public void Dispose()
  {
    _stateChangedRegistration?.Dispose();
    GC.SuppressFinalize(this);
  }

  protected override void OnInitialized()
  {
    base.OnInitialized();
    _stateChangedRegistration = MetricsState.OnStateChanged(async () =>
    {
      ComputeQuality();
      await InvokeAsync(StateHasChanged);
    });
  }

  private void ComputeQuality()
  {
    if (MetricsState.CurrentMetrics is null)
    {
      _quality = ConnectionQuality.Good;
      _iconColor = Color.Success;
      return;
    }

    var latencyMs = MetricsState.CurrentLatency.TotalMilliseconds;
    var fps = MetricsState.CurrentMetrics.Fps;

    var latencyQuality = latencyMs switch
    {
      < 50 => ConnectionQuality.Excellent,
      < 100 => ConnectionQuality.Good,
      < 200 => ConnectionQuality.Fair,
      _ => ConnectionQuality.Poor,
    };

    var fpsQuality = fps switch
    {
      >= 25 => ConnectionQuality.Excellent,
      >= 15 => ConnectionQuality.Good,
      >= 5 => ConnectionQuality.Fair,
      _ => ConnectionQuality.Poor,
    };

    // Overall quality is the worst of the two.
    _quality = (ConnectionQuality)Math.Max((int)latencyQuality, (int)fpsQuality);

    _iconColor = _quality switch
    {
      ConnectionQuality.Excellent => Color.Success,
      ConnectionQuality.Good => Color.Success,
      ConnectionQuality.Fair => Color.Warning,
      ConnectionQuality.Poor => Color.Error,
      _ => Color.Default,
    };
  }
}
