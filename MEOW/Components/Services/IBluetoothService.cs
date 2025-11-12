using MEOW.Components.Enums;
using MEOW.Components.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MEOW.Components.Services;

public interface IBluetoothService
{
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
    public event Action? PeerConnected;
    public event Action<byte[]>? DeviceDataReceived;

    ObservableCollection<MeowDevice> Devices { get; }
    Task<bool> ScanAsync();
    Task KeepScanning(TimeSpan timeSpan);
    Task ConnectAsync(MeowDevice device);

    int GetConnectedDevicesCount();
    List<string> GetConnectedDeviceName();
    void StopConnection();
    
    Task<(bool anySuccess, List<Exception> allErrors)> SendToAllAsync(byte[] data);


    Task StartAdvertisingAsync(string name);
    Task StopAdvertisingAsync();
}