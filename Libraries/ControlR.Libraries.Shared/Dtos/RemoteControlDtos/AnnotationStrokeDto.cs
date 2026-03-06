namespace ControlR.Libraries.Shared.Dtos.RemoteControlDtos;

[MessagePackObject(keyAsPropertyName: true)]
public record AnnotationStrokeDto(
    float[] PointsX,
    float[] PointsY,
    string Color,
    float Thickness,
    Guid StrokeId);
