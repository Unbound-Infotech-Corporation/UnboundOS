using UnboundOS.Core.Abstractions;
using Windows.UI.ViewManagement;

namespace UnboundOS.App.Motion;

/// <summary>Reads Windows Animation effects / Show animations.</summary>
public sealed class WindowsAnimationPreference : ISystemAnimationPreference
{
    private readonly UISettings _settings = new();

    public bool AnimationsEnabled
    {
        get
        {
            try
            {
                return _settings.AnimationsEnabled;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
