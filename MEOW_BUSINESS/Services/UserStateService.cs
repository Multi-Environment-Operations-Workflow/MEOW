namespace MEOW_BUSINESS.Services;

public class UserStateService(IAppPreferences appPreferences) : IUserStateService
{
    string _name = String.Empty;
    public string GetName()
    {
        if (string.IsNullOrEmpty(_name))
            _name = appPreferences.Get("username", String.Empty);
        return _name;
    }

    public void SetName(string name)
    {
        _name = name;
        appPreferences.Set("username", name);
    }

    public bool ResetState()
    {
        try
        {
            appPreferences.Remove("username");
            _name = String.Empty;
            return true;
        }
        catch
        {
            return false;
        }
    }
}