#if ANDROID
using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Android;

namespace MEOW.Components.Services;

public class AndroidNotificationManagerService : INotificationManagerService
{
    private int _messageId = 0;
    private bool _hasNotificationsPermission;
    private readonly Context _context;
    private const string CHANNEL_ID = "meow_notifications_channel";
    private const string CHANNEL_NAME = "MEOW Notifications";
    private const string CHANNEL_DESCRIPTION = "Notifications for MEOW app";

    public event EventHandler? NotificationReceived;

    public AndroidNotificationManagerService()
    {
        _context = Android.App.Application.Context;

        // Check if we have notification permission
        _hasNotificationsPermission = false;

        // Create notification channel (required for Android 8.0+)
        CreateNotificationChannel();
    }

    public async void SendNotification(string title, string message, DateTime? notifyTime = null)
    {
        if (!_hasNotificationsPermission)
        {
            if (!await CheckPermissions())
            {
                throw new PermissionException("App doesn't have permissions to send notifications.");
            }
            else
            {
                _hasNotificationsPermission = true;
            }
        }

        _messageId++;

        var intent = new Intent(_context, typeof(MainActivity));
        intent.PutExtra("title", title);
        intent.PutExtra("message", message);

        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
        var pendingIntent = PendingIntent.GetActivity(_context, _messageId, intent, pendingIntentFlags);

        // Build the notification
        var builder = new NotificationCompat.Builder(_context, CHANNEL_ID)
            .SetContentIntent(pendingIntent)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetSmallIcon(GetNotificationIcon())
            .SetAutoCancel(true)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetDefaults((int)NotificationDefaults.All);

        var notification = builder.Build();

        var notificationManager = NotificationManagerCompat.From(_context);

        if (notifyTime != null)
        {
            // Schedule notification for future time
            ScheduleNotification(title, message, notifyTime.Value, _messageId);
        }
        else
        {
            // Show notification immediately
            notificationManager.Notify(_messageId, notification);
        }
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

    private void CreateNotificationChannel()
    {
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High)
            {
                Description = CHANNEL_DESCRIPTION
            };

            var notificationManager = _context.GetSystemService(Context.NotificationService) as NotificationManager;
            notificationManager?.CreateNotificationChannel(channel);
        }
    }

    private bool CheckNotificationPermission()
    {
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
        {
            // Android 13+ requires runtime permission for notifications
            return _context.CheckSelfPermission(Manifest.Permission.PostNotifications) == Android.Content.PM.Permission.Granted;
        }

        // For versions below Android 13, notifications are granted by default
        return true;
    }

    async Task<bool> CheckPermissions()
    {
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();

        switch (status)
        {
            case PermissionStatus.Granted:
                return true;
            case PermissionStatus.Denied:
                {
                    if (Permissions.ShouldShowRationale<Permissions.PostNotifications>())
                    {
                        var page = Microsoft.Maui.Controls.Application.Current?.Windows[0]?.Page;
                        if (page != null)
                        {
                            await page.DisplayAlert(
                                "Notification Access Required",
                                "This app needs access to notifications to function",
                                "OK"
                            );
                        }
                    }
                    break;
                }
        }

        status = await Permissions.RequestAsync<Permissions.PostNotifications>();
        return status == PermissionStatus.Granted;
    }

    private int GetNotificationIcon()
    {
        // Use the app's launcher icon or a custom notification icon
        // You might need to create a transparent notification icon in your Resources/drawable folder
        var iconResource = _context.Resources?.GetIdentifier("icon", "drawable", _context.PackageName) ?? 0;
        return iconResource != 0 ? iconResource : Android.Resource.Drawable.SymDefAppIcon;
    }

    private void ScheduleNotification(string title, string message, DateTime notifyTime, int notificationId)
    {
        var alarmManager = _context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager == null) return;

        var intent = new Intent(_context, typeof(NotificationReceiver));
        intent.PutExtra("title", title);
        intent.PutExtra("message", message);
        intent.PutExtra("notificationId", notificationId);

        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
        var pendingIntent = PendingIntent.GetBroadcast(_context, notificationId, intent, pendingIntentFlags);

        var triggerTime = GetTimestampInMillis(notifyTime);

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
        {
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTime, pendingIntent);
        }
        else
        {
            alarmManager.SetExact(AlarmType.RtcWakeup, triggerTime, pendingIntent);
        }
    }

    private long GetTimestampInMillis(DateTime dateTime)
    {
        var utcTime = dateTime.ToUniversalTime();
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(utcTime - epoch).TotalMilliseconds;
    }
}

// Broadcast receiver for scheduled notifications
[BroadcastReceiver(Enabled = true, Exported = false)]
public class NotificationReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        var title = intent.GetStringExtra("title");
        var message = intent.GetStringExtra("message");
        var notificationId = intent.GetIntExtra("notificationId", 0);

        if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(message))
        {
            var notificationManager = new AndroidNotificationManagerService();
            notificationManager.SendNotification(title, message);
        }
    }
}
#endif