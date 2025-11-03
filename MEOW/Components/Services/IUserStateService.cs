namespace MEOW.Components.Services;

public interface IUserStateService
{
    public string GetName();
    public bool IsLoggedIn() => !string.IsNullOrEmpty(GetName());
    public void SetName(string name);
    public bool ResetState();
}