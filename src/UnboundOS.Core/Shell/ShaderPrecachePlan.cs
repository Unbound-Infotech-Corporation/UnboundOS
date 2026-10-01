using UnboundOS.Core.Models;

namespace UnboundOS.Core.Shell;

/// <summary>
/// Plans a title's own shader warmup. UnboundOS never hooks D3D or Vulkan.
/// </summary>
public static class ShaderPrecachePlan
{
    public static string Describe(GameProfile? profile)
    {
        if (profile is null || !profile.ShaderPrecache)
        {
            return "Shader pre-cache is off. UnboundOS does not hook D3D or Vulkan.";
        }

        return "Would ask this title for its own shader warmup if it exposes one. UnboundOS does not hook D3D or Vulkan and does not inject into the game.";
    }
}
