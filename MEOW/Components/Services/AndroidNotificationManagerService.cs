namespace MEOW.Components.Services;

public class AndroidNotificationManagerService: INotificationManagerService
{
    public event EventHandler? NotificationReceived;
    public void SendNotification(string title, string message, DateTime? notifyTime = null)
    {
        throw new NotImplementedException();
    }

    public void ReceiveNotification(string title, string message)
    {
        throw new NotImplementedException();
    }
}