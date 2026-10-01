namespace UnboundOS.Core.Abstractions;

/// <summary>Windows "Animation effects" / Show animations preference.</summary>
public interface ISystemAnimationPreference
{
    bool AnimationsEnabled { get; }
}
