#if IOS
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using CoreBluetooth;
using Foundation;

namespace MEOW.Components.Services;

public class IOSBluetoothService : NSObject, IBluetoothService, ICBPeripheralManagerDelegate
{
    private readonly IBluetoothLE _bluetooth = CrossBluetoothLE.Current;
    private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;

    public ObservableCollection<object> Devices { get; } = new();
    
    private CBPeripheralManager? _peripheralManager;
    private CBUUID? _serviceUuid;

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
            if (!foundDevices.Contains(a.Device))
            {
                foundDevices.Add(a.Device);
                Devices.Add(a.Device);
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

        _peripheralManager.StartAdvertising(advertisementData);

        await Task.CompletedTask;
    }

    public async Task StopAdvertisingAsync()
    {
        _peripheralManager?.StopAdvertising();
        await Task.CompletedTask;
    }

    [Export("peripheralManagerDidUpdateState:")]
    public void UpdatedState(CBPeripheralManager peripheral)
    {
        Console.WriteLine($"Peripheral state changed: {peripheral.State}");
        if (peripheral.State != CBManagerState.PoweredOn)
            Console.WriteLine("⚠️ Bluetooth not ready for advertising.");
    }
}
#endif