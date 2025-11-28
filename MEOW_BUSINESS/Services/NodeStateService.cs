
namespace MEOW_BUSINESS.Services;

public class NodeStateService()
{
    static readonly Random random = new (DateTime.Now.Millisecond);

    public static int MessageCount { get; set; }
    
    public static int[] LocalVectorClock { get; set; } = new int[byte.MaxValue + 1];
    
    public static string Name { get; private set; }
    public static byte Id { get; private set; } = (byte)random.Next(0, byte.MaxValue);
    
    public static string GetName()
    {
        if (string.IsNullOrEmpty(Name))
            Name = MeowPreferences.Get("username", String.Empty);
        return Name;
    }
    
    public static bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(GetName());
    }

    public static void SetName(string name)
    {
        Name = name;
        MeowPreferences.Set("username", name);
    }

    public static bool ResetState()
    {
        try
        {
            MeowPreferences.Remove("username");
            Name = String.Empty;
            return true;
        }
        catch
        {
            return false;
        }
    }
}