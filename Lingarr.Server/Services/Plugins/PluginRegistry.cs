using Lingarr.Contracts.Interfaces.Plugins;

namespace Lingarr.Server.Services.Plugins;

public sealed class PluginRegistry : IPluginRegistry
{
    private readonly List<RegisteredPlugin> _plugins;
    private readonly Dictionary<string, RegisteredPlugin> _byProvider;

    public PluginRegistry(
        IEnumerable<IPluginManifest> manifests,
        PluginLoader pluginLoader,
        ILogger<PluginRegistry> logger)
    {
        _plugins = new List<RegisteredPlugin>();
        _byProvider = new Dictionary<string, RegisteredPlugin>(StringComparer.OrdinalIgnoreCase);

        var loadedPlugins = new Dictionary<string, RegisteredPlugin>(StringComparer.OrdinalIgnoreCase);
        foreach (var loadedPlugin in pluginLoader.LoadedPlugins)
        {
            loadedPlugins[loadedPlugin.Manifest.Provider] = loadedPlugin;
        }

        foreach (var manifest in manifests)
        {
            if (_byProvider.TryGetValue(manifest.Provider, out var registered))
            {
                logger.LogWarning(
                    "Provider {Provider} is already registered by {ExistingType}; ignoring manifest {ManifestType}.",
                    manifest.Provider,
                    registered.Manifest.GetType().FullName,
                    manifest.GetType().FullName);
                continue;
            }

            var loadedPlugin = loadedPlugins.GetValueOrDefault(manifest.Provider);

            var entry = new RegisteredPlugin
            {
                Manifest = manifest,
                IsBuiltIn = loadedPlugin is null,
                SourceFile = loadedPlugin?.SourceFile,
                ImplementationType = loadedPlugin?.ImplementationType
            };

            _plugins.Add(entry);
            _byProvider[manifest.Provider] = entry;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RegisteredPlugin> All => _plugins;

    /// <inheritdoc />
    public RegisteredPlugin? Find(string provider)
    {
        if (_byProvider.TryGetValue(provider, out var entry))
        {
            return entry;
        }
        return null;
    }
}
