using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Core.Stream;

namespace UnboundOS.Infrastructure.Stream;

public sealed class StreamCanvasService : IStreamCanvas
{
    public StreamCropPlan BuildCropPlan(StreamPreferences preferences) =>
        StreamCropCalculator.BuildCenter16x9(preferences);

    public string BuildObsInstructions(StreamCropPlan plan, StreamPreferences preferences) =>
        $"""
        {Branding.ProductName} Ultrawide Stream Pipeline
        {Branding.CompanyName}

        1. Keep Windows display at {plan.SourceWidth}×{plan.SourceHeight} (native).
        2. In OBS, add Game Capture / Display Capture.
        3. Add a Crop/Pad filter:
           - Left: {plan.CropX}
           - Top: {plan.CropY}
           - Right: {plan.DiscardedRightPx}
           - Bottom: {Math.Max(0, plan.SourceHeight - plan.CropY - plan.CropHeight)}
        4. Set OBS canvas & output to {plan.OutputWidth}×{plan.OutputHeight} @ {preferences.OutputFps:0} FPS.
        5. Downscale filter: Lanczos
        6. Optional Sharpen: {preferences.SharpenAmount:0.00}
        7. Twitch bitrate target: {preferences.TargetBitrateKbps} Kbps

        Viewer safe zone: {plan.CropWidth}×{plan.CropHeight} centered.
        {plan.Guidance}

        Machine-readable plan:
        {plan.ObsSceneJson}
        """;
}
