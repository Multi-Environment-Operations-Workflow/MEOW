#if IOS
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using CoreBluetooth;
using Foundation;
using MEOW.Components.Enums;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

public class IOSBluetoothService : NSObject, IBluetoothService, ICBPeripheralManagerDelegate
{
    private readonly IBluetoothLE _bluetooth = CrossBluetoothLE.Current;
    private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;

    public ObservableCollection<MeowDevice> Devices { get; } = new();
    
    private CBPeripheralManager? _peripheralManager;
    private CBUUID? _serviceUuid;

    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;

    /// <summary>
    /// Scans the surrounding area for Bluetooth devices.
    /// Only devices with names starting with "(MEOW) " are added to the Devices collection
    /// </summary>
    /// <returns>True if the scan was finished successfully.</returns>
    /// <exception cref="InvalidOperationException">If Bluetooth is not initialized.</exception>
    /// <exception cref="Exception">If Bluetooth is off.</exception>
    public async Task<bool> ScanAsync()
    {
        Devices.Clear();
        if (_bluetooth == null || _adapter == null)
            throw new InvalidOperationException("Bluetooth not initialized");
        if (!_bluetooth.IsOn)
            throw new Exception("Bluetooth is off");
        var foundDevices = new List<IDevice>();
        _adapter.DeviceDiscovered += (s, a) =>
        {
            if (foundDevices.Contains(a.Device)) return;
            foundDevices.Add(a.Device);
            
            if (a.Device.Name != null && a.Device.Name.StartsWith("(MEOW) "))
            {
                Devices.Add(new MeowDevice(a.Device.Name, a.Device.Id));
            }
        };
        await _adapter.StartScanningForDevicesAsync();
        return true;
    }

    public async Task ConnectAsync(object device)
    {
        if (device is IDevice bleDevice)
        {
            await _adapter.ConnectToDeviceAsync(bleDevice);     
        }
        else
        {
            throw new ArgumentException("Device is not a BLE device");
        }
    }

    public async Task StartAdvertisingAsync(string name, Guid serviceUuid)
    {
        _serviceUuid = CBUUID.FromString(serviceUuid.ToString());
        _peripheralManager = new CBPeripheralManager(this, null);

        var advertisementData = new NSMutableDictionary();
        advertisementData.Add(CBAdvertisement.DataLocalNameKey, new NSString(name));
        advertisementData.Add(CBAdvertisement.DataServiceUUIDsKey, NSArray.FromObjects(_serviceUuid));

        while (_peripheralManager.State != CBManagerState.PoweredOn)
        {
            await Task.Delay(100);
        }

        if (_peripheralManager.State == CBManagerState.PoweredOn)
        {
            _peripheralManager.StartAdvertising(advertisementData);
            AdvertisingStateChanged?.Invoke(AdvertisingState.Started, "Advertising started successfully.");
        }
        else
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.NotSupported, "Bluetooth not powered on or not supported.");
        }
    }


    public async Task StopAdvertisingAsync()
    {
        _peripheralManager?.StopAdvertising();
        AdvertisingStateChanged?.Invoke(AdvertisingState.Stopped, "Advertising stopped.");
        await Task.CompletedTask;
    }

    [Export("peripheralManagerDidUpdateState:")]
    public void UpdatedState(CBPeripheralManager peripheral)
    {
        Console.WriteLine($"Peripheral state changed: {peripheral.State}");
        if (peripheral.State != CBManagerState.PoweredOn)
        {
            Console.WriteLine("⚠️ Bluetooth not ready for advertising.");
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth not ready for advertising.");
        }
    }
}
#endif