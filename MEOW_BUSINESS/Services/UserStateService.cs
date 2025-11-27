using System;

namespace MEOW_BUSINESS.Services;

public class UserStateService(IMeowPreferences meowPreferences) : IUserStateService
{
    static readonly Random random = new(DateTime.Now.Millisecond);

    byte _id = (byte)random.Next(0, 255);
    string _name = String.Empty;

    public string GetName()
    {
        if (string.IsNullOrEmpty(_name))
            _name = meowPreferences.Get("username", String.Empty);
        return _name;
    }

    public byte GetId()
    {
        return _id;
    }

    public void SetName(string name)
    {
        _name = name;
        meowPreferences.Set("username", name);
    }

    public bool ResetState()
    {
        try
        {
            meowPreferences.Remove("username");
            _name = String.Empty;
            return true;
        }
        catch
        {
            return false;
        }
    }
}