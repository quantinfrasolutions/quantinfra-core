namespace UI.Interfaces;

public interface ISettingsStorage
{
    Task<string> GetValue(string key);
    Task<T> GetValue<T>(string key);
    Task SetValue(string key, string value);
    Task SetValue<T>(string key, T value);
}