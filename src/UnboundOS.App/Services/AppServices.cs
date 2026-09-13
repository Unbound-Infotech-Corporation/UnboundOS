using Microsoft.Extensions.DependencyInjection;
using UnboundOS.App.ViewModels;
using UnboundOS.Infrastructure;

namespace UnboundOS.App.Services;

public static class AppServices
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static void Initialize()
    {
        var services = new ServiceCollection();
        services.AddUnboundOs();
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<SessionViewModel>();
        services.AddTransient<NetworkViewModel>();
        services.AddTransient<StreamViewModel>();
        services.AddTransient<ProfilesViewModel>();
        services.AddTransient<ModsViewModel>();
        services.AddTransient<OverlayViewModel>();
        Services = services.BuildServiceProvider();
    }

    public static T Get<T>() where T : notnull => Services.GetRequiredService<T>();
}
