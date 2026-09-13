using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Core.Stream;
using Windows.ApplicationModel.DataTransfer;

namespace UnboundOS.App.ViewModels;

public partial class StreamViewModel : ObservableObject
{
    private readonly IStreamCanvas _canvas;

    public StreamViewModel(IStreamCanvas canvas)
    {
        _canvas = canvas;
        Rebuild();
    }

    [ObservableProperty] private int _sourceWidth = 5110;
    [ObservableProperty] private int _sourceHeight = 1400;
    [ObservableProperty] private int _outputWidth = 1920;
    [ObservableProperty] private int _outputHeight = 1080;
    [ObservableProperty] private double _outputFps = 60;
    [ObservableProperty] private int _bitrateKbps = 6000;
    [ObservableProperty] private double _sharpen = 0.35;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private string _guidance = string.Empty;
    [ObservableProperty] private string _instructions = string.Empty;
    [ObservableProperty] private string _obsJson = string.Empty;
    [ObservableProperty] private double _cropPreviewWidth = 2489;
    [ObservableProperty] private double _sourcePreviewWidth = 5110;
    [ObservableProperty] private double _previewBarCropWidth = 480;
    [ObservableProperty] private string _discardText = string.Empty;

    [RelayCommand]
    private void Rebuild()
    {
        var prefs = new StreamPreferences
        {
            SourceWidth = SourceWidth,
            SourceHeight = SourceHeight,
            OutputWidth = OutputWidth,
            OutputHeight = OutputHeight,
            OutputFps = OutputFps,
            TargetBitrateKbps = BitrateKbps,
            SharpenAmount = Sharpen
        };

        var plan = _canvas.BuildCropPlan(prefs);
        Summary = StreamCropCalculator.FormatSummary(plan);
        Guidance = plan.Guidance;
        Instructions = _canvas.BuildObsInstructions(plan, prefs);
        ObsJson = plan.ObsSceneJson;
        CropPreviewWidth = plan.CropWidth;
        SourcePreviewWidth = plan.SourceWidth;
        PreviewBarCropWidth = SourcePreviewWidth <= 0
            ? 200
            : Math.Clamp(980 * (CropPreviewWidth / SourcePreviewWidth), 120, 980);
        DiscardText = $"Cropping {plan.DiscardedLeftPx}px from each wing (L{plan.DiscardedLeftPx} / R{plan.DiscardedRightPx})";
    }

    [RelayCommand]
    private void CopyObsJson()
    {
        var data = new DataPackage();
        data.SetText(ObsJson);
        Clipboard.SetContent(data);
    }

    [RelayCommand]
    private void CopyInstructions()
    {
        var data = new DataPackage();
        data.SetText(Instructions);
        Clipboard.SetContent(data);
    }

    partial void OnSourceWidthChanged(int value) => Rebuild();
    partial void OnSourceHeightChanged(int value) => Rebuild();
    partial void OnOutputWidthChanged(int value) => Rebuild();
    partial void OnOutputHeightChanged(int value) => Rebuild();
    partial void OnBitrateKbpsChanged(int value) => Rebuild();
    partial void OnSharpenChanged(double value) => Rebuild();
}
