
using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

public interface IBluetoothService
{
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
    public event Action? PeerConnected;
    public event Action<byte[]>? DeviceDataReceived;

    ObservableCollection<MeowDevice> Devices { get; }
    Task<bool> ScanAsync();
    Task ConnectAsync(MeowDevice device);
    
    int GetConnectedDevicesCount();
    
    Task<(bool anySuccess, List<Exception> allErrors)> SendToAllAsync(byte[] data);
    
    
    Task StartAdvertisingAsync(string name);
    Task StopAdvertisingAsync();
}
