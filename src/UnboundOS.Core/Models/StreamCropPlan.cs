namespace UnboundOS.Core.Models;

public sealed class StreamCropPlan
{
    public int SourceWidth { get; init; }
    public int SourceHeight { get; init; }
    public int CropX { get; init; }
    public int CropY { get; init; }
    public int CropWidth { get; init; }
    public int CropHeight { get; init; }
    public int OutputWidth { get; init; }
    public int OutputHeight { get; init; }
    public double SourceAspect { get; init; }
    public double OutputAspect { get; init; }
    public double ScaleFactor { get; init; }
    public int DiscardedLeftPx { get; init; }
    public int DiscardedRightPx { get; init; }
    public string Guidance { get; init; } = string.Empty;
    public string ObsSceneJson { get; init; } = string.Empty;
}
