using UnboundOS.Core.Abstractions;

namespace UnboundOS.Infrastructure.Settings;

public sealed class AlwaysOnSystemAnimationPreference : ISystemAnimationPreference
{
    public bool AnimationsEnabled => true;
}

public sealed class DelegateSystemAnimationPreference(Func<bool> read) : ISystemAnimationPreference
{
    public bool AnimationsEnabled => read();
}
