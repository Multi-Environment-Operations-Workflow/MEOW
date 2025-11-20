namespace MEOW_BUSINESS.Services;

public interface IMeowPreferences
{
    string Get(string key, string defaultValue = "");
    void Set(string key, string value);
    void Remove(string key);
}