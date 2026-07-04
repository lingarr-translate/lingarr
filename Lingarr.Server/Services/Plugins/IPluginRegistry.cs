using Lingarr.Contracts.Interfaces.Plugins;

namespace Lingarr.Server.Services.Plugins;

/// <summary>
/// In-memory registry containing all registered plugin manifests.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>
    /// Returns every registered plugin in registration order.
    /// </summary>
    IReadOnlyList<RegisteredPlugin> All { get; }

    /// <summary>
    /// Returns the registered plugin with the given provider identifier, or null when none is registered.
    /// </summary>
    RegisteredPlugin? Find(string provider);
}
