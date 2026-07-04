using Lingarr.Contracts.Plugins;

namespace Lingarr.Contracts.Interfaces.Plugins;

/// <summary>
/// Describes a translation provider to the settings UI so the configuration form
/// renders from the manifest without provider specific frontend code.
/// </summary>
public interface IPluginManifest
{
    string Provider { get; }
    string DisplayName { get; }
    string? Description { get; }
    IReadOnlyList<PluginSettingField> Settings { get; }
    bool HasRequestTemplate => false;
}
