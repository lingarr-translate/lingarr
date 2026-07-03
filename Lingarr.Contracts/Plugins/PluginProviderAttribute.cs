namespace Lingarr.Contracts.Plugins;

/// <summary>
/// Marks a class as a Lingarr provider and supplies the key the plugin loader uses to register it in DI.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PluginProviderAttribute : Attribute
{
    public string Provider { get; }

    public PluginProviderAttribute(string provider)
    {
        Provider = provider;
    }
}
