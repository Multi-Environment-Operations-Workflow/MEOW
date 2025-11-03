namespace MEOW_BUSINESS.Services;

public interface IAppPreferences
{
    string Get(string key, string defaultValue);
    void Set(string key, string value);
    bool ContainsKey(string key);
    void Remove(string key);
    void Clear();
}