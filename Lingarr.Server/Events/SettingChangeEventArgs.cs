namespace Lingarr.Server.Events;

public class SettingChangeEventArgs : EventArgs
{
    public string Key { get; }
    public string Value { get; }

    public SettingChangeEventArgs(string key, string value)
    {
        Key = key;
        Value = value;
    }
}