using System.Reflection;
using System.Runtime.Loader;
using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Contracts.Translation;

namespace Lingarr.Server.Services.Plugins;

/// <summary>
/// Loads plugin assemblies from the directory configured through PLUGINS_PATH and registers
/// the translation providers and manifests they expose at startup.
/// </summary>
public sealed class PluginLoader
{
    private const int HostMajorVersion = 1;
    public const string PluginsPathEnvironmentVariable = "PLUGINS_PATH";

    private readonly ILogger<PluginLoader> _logger;
    private readonly List<RegisteredPlugin> _loadedPlugins = new();

    public IReadOnlyList<RegisteredPlugin> LoadedPlugins => _loadedPlugins;
    public bool LoadingEnabled { get; private set; }

    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger;
    }

    public void LoadPlugins(IServiceCollection services)
    {
        var pluginsPath = Environment.GetEnvironmentVariable(PluginsPathEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(pluginsPath))
        {
            _logger.LogInformation(
                "Plugin loading disabled. Set {EnvironmentVariable} to enable.",
                PluginsPathEnvironmentVariable);
            return;
        }

        if (!Directory.Exists(pluginsPath))
        {
            _logger.LogWarning(
                "{EnvironmentVariable} is set to {PluginsPath} but the directory does not exist.",
                PluginsPathEnvironmentVariable,
                pluginsPath);
            return;
        }

        LoadingEnabled = true;
        var pluginFiles = Directory.EnumerateFiles(pluginsPath, "*.dll");

        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                LoadPluginAssembly(services, pluginFile);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to load plugin {PluginFile}, skipping.",
                    pluginFile);
            }
        }
    }

    private void LoadPluginAssembly(IServiceCollection services, string pluginFile)
    {
        var assemblyName = AssemblyName.GetAssemblyName(pluginFile);
        if (IsHostAssembly(assemblyName))
        {
            return;
        }

        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginFile);

        var versionAttribute = assembly.GetCustomAttribute<LingarrPluginApiVersionAttribute>();
        if (versionAttribute is null)
        {
            _logger.LogDebug(
                "Assembly {PluginFile} has no LingarrPluginApiVersion attribute, skipping.",
                pluginFile);
            return;
        }

        if (versionAttribute.Major != HostMajorVersion)
        {
            _logger.LogWarning(
                "Plugin {PluginFile} targets plugin API version {Major}.{Minor} but the host requires major version {HostMajorVersion}, skipping.",
                pluginFile,
                versionAttribute.Major,
                versionAttribute.Minor,
                HostMajorVersion);
            return;
        }

        var exportedTypes = assembly.GetExportedTypes();
        var manifestsFromThisAssembly = new List<IPluginManifest>();
        var sourceFileName = Path.GetFileName(pluginFile);

        foreach (var type in exportedTypes)
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            var providerAttribute = type.GetCustomAttribute<PluginProviderAttribute>();
            if (providerAttribute is not null)
            {
                RegisterProvider(services, type, providerAttribute);
            }

            if (typeof(IPluginManifest).IsAssignableFrom(type))
            {
                var manifestInstance = TryCreateManifest(type, pluginFile);
                if (manifestInstance is not null)
                {
                    services.AddSingleton(typeof(IPluginManifest), manifestInstance);
                    manifestsFromThisAssembly.Add(manifestInstance);
                }
            }
        }

        foreach (var manifest in manifestsFromThisAssembly)
        {
            _loadedPlugins.Add(new RegisteredPlugin
            {
                Manifest = manifest,
                IsBuiltIn = false,
                SourceFile = sourceFileName
            });
        }

        _logger.LogInformation(
            "Loaded plugin {PluginFile} ({ManifestCount} manifest(s)).",
            pluginFile,
            manifestsFromThisAssembly.Count);
    }

    private static void RegisterProvider(
        IServiceCollection services,
        Type implementationType,
        PluginProviderAttribute providerAttribute)
    {
        if (typeof(ITranslationService).IsAssignableFrom(implementationType))
        {
            services.AddKeyedScoped(
                typeof(ITranslationService),
                providerAttribute.Provider,
                implementationType);
        }
    }

    private IPluginManifest? TryCreateManifest(Type manifestType, string pluginFile)
    {
        var parameterlessConstructor = manifestType.GetConstructor(Type.EmptyTypes);
        if (parameterlessConstructor is null)
        {
            _logger.LogWarning(
                "Plugin manifest {ManifestType} from {PluginFile} has no parameterless constructor, skipping.",
                manifestType.FullName,
                pluginFile);
            return null;
        }

        try
        {
            return (IPluginManifest?)parameterlessConstructor.Invoke(null);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to instantiate plugin manifest {ManifestType} from {PluginFile}.",
                manifestType.FullName,
                pluginFile);
            return null;
        }
    }

    private static bool IsHostAssembly(AssemblyName assemblyName)
    {
        foreach (var loadedAssembly in AssemblyLoadContext.Default.Assemblies)
        {
            if (string.Equals(
                    loadedAssembly.GetName().Name,
                    assemblyName.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
