namespace MEOW.Components.Services;

public class UserStateService : IUserStateService
{
    string _name = String.Empty;
    public string GetName()
    {
        if (string.IsNullOrEmpty(_name))
            _name = Preferences.Get("username", String.Empty);
        return _name;
    }

    public void SetName(string name)
    {
        _name = name;
        Preferences.Set("username", name);
    }

    public bool ResetState()
    {
        try
        {
            Preferences.Remove("username");
            _name = String.Empty;
            return true;
        }
        catch
        {
            return false;
        }
    }
}