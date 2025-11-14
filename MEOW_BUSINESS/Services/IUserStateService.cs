namespace MEOW_BUSINESS.Services;

public interface IUserStateService
{
    public string GetName();
    public byte GetId();
    public bool IsLoggedIn() => !string.IsNullOrEmpty(GetName());
    public void SetName(string name);
    public bool ResetState();
}