using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    
    Task<(bool anySuccess, List<Exception> allErrors)> BroadcastMessage(byte[] data);
    Task StartAdvertisingAsync();
    Task StopAdvertisingAsync();
}