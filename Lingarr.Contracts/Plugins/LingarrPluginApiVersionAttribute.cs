namespace Lingarr.Contracts.Plugins;

/// <summary>
/// Marks the Lingarr plugin API version a plugin assembly targets.
/// The plugin loader rejects any assembly whose major version does not match the host.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class LingarrPluginApiVersionAttribute : Attribute
{
    public int Major { get; }
    public int Minor { get; }

    public LingarrPluginApiVersionAttribute(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }
}
