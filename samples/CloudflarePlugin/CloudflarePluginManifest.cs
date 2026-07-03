using Lingarr.Contracts.Interfaces.Plugins;
using Lingarr.Contracts.Plugins;

namespace Lingarr.Plugin.Cloudflare;

public sealed class CloudflarePluginManifest : IPluginManifest
{
    public string Provider => "cloudflare";

    public string DisplayName => "Cloudflare Workers AI";

    public string? Description =>
        "Translates subtitles using Cloudflare Workers AI. Supports the models @cf/meta/m2m100-1.2b and @cf/meta/nllb-200.";

    public IReadOnlyList<PluginSettingField> Settings { get; } =
    [
        new()
        {
            Key = "cloudflare_account_id",
            Label = "Account ID",
            Type = PluginSettingType.Text,
            Required = true,
            Description = "Your Cloudflare account ID, visible in Account Home (dash.cloudflare.com/{account_id}/...)."
        },
        new()
        {
            Key = "cloudflare_api_token",
            Label = "API token",
            Type = PluginSettingType.Secret,
            Required = true,
            Description = "API token with read permission on the Workers AI. Generate at My Profile → API Tokens."
        },
        new()
        {
            Key = "cloudflare_model",
            Label = "Translation model",
            Type = PluginSettingType.Text,
            Default = "@cf/meta/m2m100-1.2b",
            Description = "Workers AI translation model identifier. Defaults to m2m100-1.2b; @cf/meta/nllb-200 is also available."
        }
    ];
}
