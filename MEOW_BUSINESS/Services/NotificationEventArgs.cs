namespace MEOW_BUSINESS.Services;

public class NotificationEventArgs : EventArgs
{
    public string Title { get; set; }
    public string Message { get; set; }
}