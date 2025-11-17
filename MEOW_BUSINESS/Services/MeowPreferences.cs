namespace MEOW_BUSINESS.Services;

public class MeowPreferences: IMeowPreferences
{
    public string Get(string key, string defaultValue = "")
    {
        return Preferences.Get(key, defaultValue);
    }

    public void Set(string key, string value)
    {
        Preferences.Set(key, value);
    }

    public void Remove(string key)
    {
        Preferences.Remove(key);
    }
}