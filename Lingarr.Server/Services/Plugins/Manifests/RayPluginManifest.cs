using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;

namespace Lingarr.Server.Services.Plugins.Manifests;

public sealed class RayPluginManifest : IPluginManifest
{
    public string Provider => "ray";

    public string DisplayName => "Ray Local";

    public string? Description =>
        "Translates subtitle batches with a Ray Headless server (Sunlight model). Prefer target <code>local</code> so media stays on your machine; use <code>cloud</code> only when you intend to process on Ray Cloud.";

    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = SettingKeys.Translation.Ray.Endpoint,
            Label = "Ray server URL",
            Type = PluginSettingType.Url,
            Required = true,
            Default = "http://127.0.0.1:8787",
            Description = "Base URL of the Ray Headless API (for example http://127.0.0.1:8787 when using host networking)."
        },
        new()
        {
            Key = SettingKeys.Translation.Ray.ApiKey,
            Label = "Ray API key",
            Type = PluginSettingType.Secret,
            Required = true,
            Description = "Ray server API key. Stored encrypted.",
            MinLength = 1,
            ValidationErrorMessage = "Value must not be empty"
        },
        new()
        {
            Key = SettingKeys.Translation.Ray.Target,
            Label = "Ray processing target",
            Type = PluginSettingType.Text,
            Required = true,
            Default = "local",
            Description = "Use local for on-device processing or cloud to process on Ray Cloud."
        },
        new()
        {
            Key = SettingKeys.Translation.Ray.BatchSize,
            Label = "Cues per Ray job",
            Type = PluginSettingType.Text,
            Required = true,
            Default = "200",
            Description = "Subtitle cues sent to Ray in each job (1-500)."
        },
        new()
        {
            Key = SettingKeys.Translation.Ray.JobTimeoutMinutes,
            Label = "Ray job timeout (minutes)",
            Type = PluginSettingType.Text,
            Required = true,
            Default = "180",
            Description = "Maximum wait for each Ray translation job."
        }
    ];
}
