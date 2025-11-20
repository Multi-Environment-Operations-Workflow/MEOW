using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MEOW_BUSINESS.Services;

public interface IBluetoothService
{
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
    
    public event Action? PeerConnected;
    
    public event Action<byte[]>? DeviceDataReceived;

    ObservableCollection<MeowDevice> DiscoveredDevices { get; }
    
    Task<bool> ScanAsync();
    
    Task ScanAsyncAutomatically();
    
    Task RunInBackground(TimeSpan timeSpan, Func<Task> func);
    
    Task ConnectAsync(MeowDevice device);

    List<MeowDevice> GetConnectedDevices();
    
    Task<(bool anySuccess, List<Exception> allErrors)> SendToAllAsync(byte[] data);


    Task StartAdvertisingAsync(string name);
    Task StopAdvertisingAsync();
}