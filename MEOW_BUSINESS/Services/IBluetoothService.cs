using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public interface IBluetoothService
{
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
    
    public event Action? PeerConnected;
    
    public event Action<byte[]>? DeviceDataReceived;
    
    List<MeowDevice> ConnectedDevices { get; }
    List<MeowDevice> EstablishingConnectionDevices { get; }
    
    Task<List<MeowDevice>> ScanForDevices();
    
    Task ScanAndConnectToDeviceName(string name);
    
    Task ScanAndConnectToAllFoundDevices();
    
    Task ConnectToDevice(MeowDevice device);

    List<MeowDevice> GetConnectedDevices();
    
    Task<(bool anySuccess, List<Exception> allErrors)> SendToAllAsync(byte[] data);

    public Task<int> GetRSSI(string deviceName);

    public string? get_device_name_by_id(byte userId);
    Task StartAdvertisingAsync(string name);
    Task<(bool anySuccess, List<Exception> allErrors)> BroadcastMessage(byte[] data);
    Task StartAdvertisingAsync();
    Task StopAdvertisingAsync();
}