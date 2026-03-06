using Microsoft.JSInterop;

namespace ControlR.Web.Client.Components.RemoteDisplays;

public partial class AnnotationCanvas : JsInteropableComponent
{
  private readonly string _annotationCanvasId = $"annotation-{Guid.NewGuid()}";
  private DotNetObjectReference<AnnotationCanvas>? _componentRef;
  private ElementReference _annotationCanvasRef;

  private string _selectedColor = "#ff0000";
  private float _selectedThickness = 3f;

  private readonly string[] _colors =
  [
    "#ff0000", // red
    "#2196f3", // blue
    "#4caf50", // green
    "#ffeb3b", // yellow
    "#ffffff", // white
    "#000000"  // black
  ];

  private readonly (string Label, float Thickness)[] _thicknessOptions =
  [
    ("Thin", 2f),
    ("Med", 4f),
    ("Thick", 8f)
  ];

  [Parameter]
  [EditorRequired]
  public bool IsActive { get; set; }

  [Parameter]
  [EditorRequired]
  public double CanvasWidth { get; set; }

  [Parameter]
  [EditorRequired]
  public double CanvasHeight { get; set; }

  [Parameter]
  [EditorRequired]
  public string CanvasStyle { get; set; } = string.Empty;

  [Inject]
  public required IViewerRemoteControlStream RemoteControlStream { get; init; }

  [Inject]
  public required ILogger<AnnotationCanvas> Logger { get; init; }

  [JSInvokable]
  public async Task OnStrokeCompleted(float[] pointsX, float[] pointsY, string color, float thickness)
  {
    try
    {
      if (!RemoteControlStream.IsConnected)
      {
        return;
      }

      var strokeId = Guid.NewGuid();
      await RemoteControlStream.SendAnnotationStroke(pointsX, pointsY, color, thickness, strokeId, ComponentClosing);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error while sending annotation stroke.");
    }
  }

  protected override async ValueTask DisposeAsync(bool disposing)
  {
    if (disposing)
    {
      Disposer.DisposeAll(_componentRef);
    }
    await base.DisposeAsync(disposing);
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    await base.OnAfterRenderAsync(firstRender);

    if (firstRender)
    {
      _componentRef = DotNetObjectReference.Create(this);
      await JsModule.InvokeVoidAsync("initialize", _componentRef, _annotationCanvasId);
    }
  }

  private void HandleColorSelected(string color)
  {
    _selectedColor = color;
    _ = JsModule.InvokeVoidAsync("setColor", _annotationCanvasId, color);
  }

  private void HandleThicknessSelected(float thickness)
  {
    _selectedThickness = thickness;
    _ = JsModule.InvokeVoidAsync("setThickness", _annotationCanvasId, thickness);
  }

  private async Task HandleClearClicked()
  {
    try
    {
      await JsModule.InvokeVoidAsync("clearCanvas", _annotationCanvasId);

      if (RemoteControlStream.IsConnected)
      {
        await RemoteControlStream.SendAnnotationClear(ComponentClosing);
      }
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error while clearing annotations.");
    }
  }
}
