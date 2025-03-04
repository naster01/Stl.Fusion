namespace Stl.Plugins;

public static class ServiceProviderExt
{
    public static IPluginHost Plugins(this IServiceProvider services)
        => services.GetRequiredService<IPluginHost>();
}
