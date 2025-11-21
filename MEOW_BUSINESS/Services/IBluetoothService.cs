using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public interface IBluetoothService
{
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
    
    public event Action? PeerConnected;
    
    public event Action<byte[]>? DeviceDataReceived;
    
    Task<List<MeowDevice>> ScanForDevices();
    
    Task ScanAndConnectToAllFoundDevices();
    
    Task RunInBackground(TimeSpan timeSpan, Func<Task> func);
    
    Task ConnectToDevice(MeowDevice device);

    List<MeowDevice> GetConnectedDevices();
    
    Task<(bool anySuccess, List<Exception> allErrors)> SendToAllAsync(byte[] data);


    Task StartAdvertisingAsync(string name);
    Task StopAdvertisingAsync();
}