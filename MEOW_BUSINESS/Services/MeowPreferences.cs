namespace MEOW_BUSINESS.Services;

public class MeowPreferences
{
    public static string Get(string key, string defaultValue = "")
    {
        return Preferences.Get(key, defaultValue);
    }

    public static void Set(string key, string value)
    {
        Preferences.Set(key, value);
    }

    public static void Remove(string key)
    {
        Preferences.Remove(key);
    }
}