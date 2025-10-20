namespace MEOW.Components.Services;

public interface IUserStateService
{
    public string GetName();
    public void SetName(string name);
    public bool ResetState();
}