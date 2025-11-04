#if IOS
using Foundation;
using UserNotifications;

namespace MEOW.Components.Services;

public class IOSNotificationManagerService : INotificationManagerService
{
    int _messageId = 0;
    bool _hasNotificationsPermission;

    public event EventHandler? NotificationReceived;

    public IOSNotificationManagerService()
    {
        // Create a UNUserNotificationCenterDelegate to handle incoming messages.
        UNUserNotificationCenter.Current.Delegate = new IOSNotificationReceiver();

        // Request permission to use local notifications.
        UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert,
            (approved, err) => { _hasNotificationsPermission = approved; });
    }

    public void SendNotification(string title, string message, DateTime? notifyTime = null)
    {
        if (!_hasNotificationsPermission)
        {
            throw new PermissionException("App doesn't have permissions to send notifications.");
        }

        _messageId++;
        var content = new UNMutableNotificationContent()
        {
            Title = title,
            Subtitle = "",
            Body = message,
            Badge = 1
        };

        UNNotificationTrigger trigger;
        if (notifyTime != null)
            // Create a calendar-based trigger.
            trigger = UNCalendarNotificationTrigger.CreateTrigger(GetNSDateComponents(notifyTime.Value), false);
        else
            // Create a time-based trigger, interval is in seconds and must be greater than 0.
            trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(30, false);

        var request = UNNotificationRequest.FromIdentifier(_messageId.ToString(), content, trigger);
        UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
        {
            if (err != null)
                throw new Exception($"Failed to schedule notification: {err}");
        });
    }

    public void ReceiveNotification(string title, string message)
    {
        var args = new NotificationEventArgs()
        {
            Title = title,
            Message = message
        };
        NotificationReceived?.Invoke(null, args);
    }

    private NSDateComponents GetNSDateComponents(DateTime dateTime)
    {
        return new NSDateComponents
        {
            Month = dateTime.Month,
            Day = dateTime.Day,
            Year = dateTime.Year,
            Hour = dateTime.Hour,
            Minute = dateTime.Minute,
            Second = dateTime.Second
        };
    }
}
#endif