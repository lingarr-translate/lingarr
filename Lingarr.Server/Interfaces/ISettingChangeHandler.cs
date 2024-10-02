namespace Lingarr.Server.Interfaces;

public interface ISettingChangeHandler
{
    /// <summary>
    /// Handles actions required after modifying a specific setting.
    /// </summary>
    /// <param name="key">The key of the modified setting.</param>
    /// <param name="value">The new value of the modified setting.</param>
    void HandleSettingChange(string key, string value);
}