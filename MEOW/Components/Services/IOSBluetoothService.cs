using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using System.Collections.ObjectModel;

namespace MEOW.Components.Services;

public class IOSBluetoothService : IBluetoothService
{
    private readonly IBluetoothLE _bluetooth = CrossBluetoothLE.Current;
    private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;

    public ObservableCollection<object> Devices { get; } = new();

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
            try
            {
                await _adapter.ConnectToDeviceAsync(bleDevice);
            }
            catch (DeviceConnectionException)
            {
                // Handle connection error
            }
        }
        else
        {
            throw new ArgumentException("Device is not a BLE device");
        }
    }
}