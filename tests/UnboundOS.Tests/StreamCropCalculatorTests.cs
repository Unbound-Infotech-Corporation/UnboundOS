using UnboundOS.Core.Models;
using UnboundOS.Core.Stream;

namespace UnboundOS.Tests;

public sealed class StreamCropCalculatorTests
{
    [Fact]
    public void Ultrawide_5110x1400_CentersExact16x9Window()
    {
        var plan = StreamCropCalculator.BuildCenter16x9(new StreamPreferences
        {
            SourceWidth = 5110,
            SourceHeight = 1400,
            OutputWidth = 1920,
            OutputHeight = 1080,
            OutputFps = 60,
            TargetBitrateKbps = 6000,
            SharpenAmount = 0.35
        });

        Assert.Equal(1400, plan.CropHeight);
        Assert.Equal(2489, plan.CropWidth);
        Assert.Equal(0, plan.CropY);
        Assert.Equal(5110, plan.DiscardedLeftPx + plan.CropWidth + plan.DiscardedRightPx);
        Assert.True(plan.CropX + plan.CropWidth <= 5110);
        Assert.Contains("crop_filter", plan.ObsSceneJson);
    }

    [Fact]
    public void TallSource_CentersHorizontallyFullWidth()
    {
        var plan = StreamCropCalculator.BuildCenter16x9(new StreamPreferences
        {
            SourceWidth = 1080,
            SourceHeight = 1920,
            OutputWidth = 1920,
            OutputHeight = 1080
        });

        Assert.Equal(1080, plan.CropWidth);
        Assert.Equal(0, plan.CropX);
        Assert.True(plan.CropY >= 0);
        Assert.True(plan.CropY + plan.CropHeight <= 1920);
    }
}
