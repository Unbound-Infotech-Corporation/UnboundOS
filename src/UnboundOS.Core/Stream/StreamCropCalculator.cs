using System.Globalization;
using System.Text.Json;
using UnboundOS.Core.Models;

namespace UnboundOS.Core.Stream;

/// <summary>
/// Pure crop/scale math for ultrawide → Twitch 16:9 pipelines.
/// Keeps the desktop at native resolution; never asks the user to lower monitor res.
/// </summary>
public static class StreamCropCalculator
{
    public static StreamCropPlan BuildCenter16x9(StreamPreferences prefs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prefs.SourceWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prefs.SourceHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prefs.OutputWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prefs.OutputHeight);

        var sourceAspect = prefs.SourceWidth / (double)prefs.SourceHeight;
        var outputAspect = prefs.OutputWidth / (double)prefs.OutputHeight;

        int cropW;
        int cropH;
        int cropX;
        int cropY;

        // Fit a 16:9 (or target) window inside the source, prioritizing full height on ultrawides.
        if (sourceAspect > outputAspect)
        {
            cropH = prefs.SourceHeight;
            cropW = (int)Math.Round(cropH * outputAspect);
            cropX = Math.Max(0, (prefs.SourceWidth - cropW) / 2);
            cropY = 0;
        }
        else
        {
            cropW = prefs.SourceWidth;
            cropH = (int)Math.Round(cropW / outputAspect);
            cropX = 0;
            cropY = Math.Max(0, (prefs.SourceHeight - cropH) / 2);
        }

        cropW = Math.Min(cropW, prefs.SourceWidth - cropX);
        cropH = Math.Min(cropH, prefs.SourceHeight - cropY);

        var scale = Math.Min(
            prefs.OutputWidth / (double)cropW,
            prefs.OutputHeight / (double)cropH);

        var discardedLeft = cropX;
        var discardedRight = Math.Max(0, prefs.SourceWidth - cropX - cropW);

        var plan = new StreamCropPlan
        {
            SourceWidth = prefs.SourceWidth,
            SourceHeight = prefs.SourceHeight,
            CropX = cropX,
            CropY = cropY,
            CropWidth = cropW,
            CropHeight = cropH,
            OutputWidth = prefs.OutputWidth,
            OutputHeight = prefs.OutputHeight,
            SourceAspect = sourceAspect,
            OutputAspect = outputAspect,
            ScaleFactor = scale,
            DiscardedLeftPx = discardedLeft,
            DiscardedRightPx = discardedRight,
            Guidance =
                $"Keep gameplay HUD inside the center {cropW}×{cropH} window. " +
                $"Sides lose ~{discardedLeft}px left / {discardedRight}px right. " +
                $"Downscale once with Lanczos, then light sharpen ({prefs.SharpenAmount:0.00}).",
            ObsSceneJson = BuildObsFragment(prefs, cropX, cropY, cropW, cropH)
        };

        return plan;
    }

    private static string BuildObsFragment(StreamPreferences prefs, int x, int y, int w, int h)
    {
        var payload = new
        {
            product = Branding.ProductName,
            company = Branding.CompanyName,
            mode = "UltrawideStream",
            source = new { width = prefs.SourceWidth, height = prefs.SourceHeight },
            crop = new { left = x, top = y, width = w, height = h, right = prefs.SourceWidth - x - w, bottom = prefs.SourceHeight - y - h },
            output = new { width = prefs.OutputWidth, height = prefs.OutputHeight, fps = prefs.OutputFps, bitrateKbps = prefs.TargetBitrateKbps },
            filters = new object[]
            {
                new { type = "crop_filter", left = x, top = y, right = prefs.SourceWidth - x - w, bottom = prefs.SourceHeight - y - h },
                new { type = "scale_filter", width = prefs.OutputWidth, height = prefs.OutputHeight, sampling = "lanczos" },
                new { type = "sharpness_filter", sharpness = prefs.SharpenAmount }
            },
            note = "Import numbers into OBS Game Capture → Filters. Do not lower Windows display resolution."
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public static string FormatSummary(StreamCropPlan plan) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{plan.SourceWidth}×{plan.SourceHeight} → crop {plan.CropWidth}×{plan.CropHeight} @ ({plan.CropX},{plan.CropY}) → {plan.OutputWidth}×{plan.OutputHeight} (×{plan.ScaleFactor:0.000})");
}
