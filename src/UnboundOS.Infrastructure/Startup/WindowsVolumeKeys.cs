using System.Runtime.InteropServices;
using UnboundOS.Core.Abstractions;

namespace UnboundOS.Infrastructure.Startup;

/// <summary>Sends the hardware volume keys so Windows can show its own OSD.</summary>
public sealed class WindowsVolumeKeys : IVolumeKeys
{
    private const byte VkMute = 0xAD;
    private const byte VkDown = 0xAE;
    private const byte VkUp = 0xAF;
    private const uint KeyeventfExtendedkey = 1;
    private const uint KeyeventfKeyup = 2;

    public Task<(bool Succeeded, string Message)> VolumeUpAsync() => Pulse(VkUp, "Volume up.");

    public Task<(bool Succeeded, string Message)> VolumeDownAsync() => Pulse(VkDown, "Volume down.");

    public Task<(bool Succeeded, string Message)> MuteAsync() => Pulse(VkMute, "Mute toggled.");

    private static Task<(bool Succeeded, string Message)> Pulse(byte key, string ok)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult((false, "Volume keys run on Windows."));
        }

        try
        {
            keybd_event(key, 0, KeyeventfExtendedkey, UIntPtr.Zero);
            keybd_event(key, 0, KeyeventfExtendedkey | KeyeventfKeyup, UIntPtr.Zero);
            return Task.FromResult((true, ok));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
