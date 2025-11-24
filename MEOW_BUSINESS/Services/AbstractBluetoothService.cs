using System.Collections.ObjectModel;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public abstract class AbstractBluetoothService
{
    protected readonly IBluetoothLE BluetoothLe = CrossBluetoothLE.Current;
    protected readonly IAdapter Adapter = CrossBluetoothLE.Current.Adapter;
    private readonly Dictionary<byte, string> _userIdToDeviceName = new();

    public ObservableCollection<MeowDevice> DiscoveredDevices { get; } = new();

    protected readonly List<MeowDevice> ConnectedDevices = new();
    
    protected readonly List<MeowDevice> EstablishingConnectionDevices = new();

    public event Action<byte[]>? DeviceDataReceived;

    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;


    public event Action? PeerConnected;
    
    protected AbstractBluetoothService(IErrorService errorService)
    {
        Adapter.DeviceConnected += (s, a) =>
        {
            var device = a.Device;
            if (ConnectedDevices.All(cd => cd.Id != device.Id))
            {
                ConnectedDevices.Add(new MeowDevice(device.Name ?? string.Empty, device.Id, device));
            }
            ConnectedDevices.Add(EstablishingConnectionDevices.FirstOrDefault(d => d.Id == device.Id)!);
            EstablishingConnectionDevices.RemoveAll(d => d.Id == device.Id);
            PeerConnected?.Invoke();
        };
        
        Adapter.DeviceConnectionLost += (s, a) =>
        {
            var device = a.Device;
            ConnectedDevices.RemoveAll(cd => cd.Id == device.Id);
        };
        
        Adapter.DeviceConnectionError += (s, a) =>
        {
            var device = a.Device;
            ConnectedDevices.RemoveAll(cd => cd.Id == device.Id);
            EstablishingConnectionDevices.RemoveAll(d => d.Id == device.Id);
            errorService.Add(new Exception($"Connection error with device {device.Name}"));
        };

        try
        {
            var currentlyConnected = Adapter?.ConnectedDevices;
            if (currentlyConnected != null)
            {
                foreach (var dev in currentlyConnected)
                {
                    if (ConnectedDevices.All(cd => cd.Id != dev.Id))
                    {
                        ConnectedDevices.Add(new MeowDevice(dev.Name ?? string.Empty, dev.Id, dev));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to populate connected devices: {ex}");
        }
    }

    public async Task<bool> ScanAsync()
    {
        DiscoveredDevices.Clear();
        if (BluetoothLe == null || Adapter == null)
            throw new InvalidOperationException("Bluetooth not initialized");
        if (!BluetoothLe.IsOn)
            throw new Exception("Bluetooth is off");

        var foundDevices = new List<IDevice>();

        EventHandler<DeviceEventArgs> discoveredHandler = (s, a) =>
        {
            if (foundDevices.Contains(a.Device)) return;
            foundDevices.Add(a.Device);

            if (a.Device.Name != null)
            {
                DiscoveredDevices.Add(new MeowDevice(a.Device.Name, a.Device.Id, a.Device));
            }
        };

        Adapter.DeviceDiscovered += discoveredHandler;
        try
        {
            await Adapter.StartScanningForDevicesAsync([ChatUuids.ChatService]);
        }
        finally
        {
            Adapter.DeviceDiscovered -= discoveredHandler;
        }
        
        return true;
    }


    public async Task ScanAsyncAutomatically()
    {
        await ScanAsync();
        foreach (var device in DiscoveredDevices)
        {
            await ConnectAsync(device);
        }
    }

    public async Task RunInBackground(TimeSpan timeSpan, Func<Task> func)
    {
        var periodicTimer = new PeriodicTimer(timeSpan);
        while (await periodicTimer.WaitForNextTickAsync())
        {
            await func();
        }
    }
    
    /// <summary>
    /// Gets the list of currently connected devices.
    /// </summary>
    /// <returns>List of connected MeowDevice instances.</returns>
    public List<MeowDevice> GetConnectedDevices()
    {
        return ConnectedDevices.ToList();
    }

    public async Task<(bool, List<Exception>)> SendToAllAsync(byte[] data)
    {
        var anySuccess = false;
        var exceptions = new List<Exception>();
        var targets = ConnectedDevices.ToList();

        foreach (var device in targets)
        {
            try
            {
                var native = device.NativeDevice;
                
                if (native == null)
                    throw new Exception($"Native device is null for {device.Name}");

                if (Adapter.ConnectedDevices.All(d => d.Id != native.Id))
                {
                    await Adapter.ConnectToDeviceAsync(native).ConfigureAwait(false);
                }

                var services = await native.GetServicesAsync().ConfigureAwait(false);

                var expectedServiceId = ChatUuids.ChatService.ToString();
                var service = services.FirstOrDefault(s => string.Equals(s.Id.ToString(), expectedServiceId, StringComparison.OrdinalIgnoreCase));

                if (service == null)
                {
                    Console.WriteLine($"Service {expectedServiceId} not found on {device.Name}. Available services:");
                    foreach (var s in services)
                    {
                        Console.WriteLine($"  - {s.Id}");
                    }
                    throw new Exception($"Service {expectedServiceId} not found on {device.Name}.");
                }

                var characteristics = await service.GetCharacteristicsAsync().ConfigureAwait(false);
                var expectedCharId = ChatUuids.MessageReceiveCharacteristic.ToString();
                var characteristic = characteristics.FirstOrDefault(c => string.Equals(c.Id.ToString(), expectedCharId, StringComparison.OrdinalIgnoreCase));

                if (characteristic == null)
                {
                    Console.WriteLine($"Characteristic {expectedCharId} not found on {device.Name}. Available characteristics:");
                    foreach (var c in characteristics)
                    {
                        Console.WriteLine($"  - {c.Id}");
                    }
                    throw new Exception($"Characteristic {expectedCharId} not found on {device.Name}.");
                }

                await characteristic.WriteAsync(data).ConfigureAwait(false);
                anySuccess = true;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        return (anySuccess, exceptions);
    }

    public async Task ConnectAsync(MeowDevice device)
    {
        EstablishingConnectionDevices.Add(device);
        if (device?.NativeDevice == null)
            throw new ArgumentNullException(nameof(device));

        await Adapter.ConnectToDeviceAsync(device.NativeDevice).ConfigureAwait(false);

        var services = await device.NativeDevice.GetServicesAsync().ConfigureAwait(false);
        foreach (var service in services)
        {
            var characteristics = await service.GetCharacteristicsAsync().ConfigureAwait(false);

            foreach (var characteristic in characteristics)
            {
                if (characteristic.CanUpdate)
                {
                    characteristic.ValueUpdated += (s, a) =>
                    {
                        var data = a.Characteristic?.Value;
                        if (data != null && data.Length > 0)
                        {
                            DeviceDataReceived?.Invoke(data);
                        }
                    };

                    await characteristic.StartUpdatesAsync().ConfigureAwait(false);
                }
            }
        }
    }
   
    public string? get_device_name_by_id(byte userId)
    {
        if (_userIdToDeviceName.TryGetValue(userId, out var name))
            return name;

        return null;
    }


public async Task<int> GetRSSI(string deviceName)
{
    // Find enheden (uden at kaste fejl)
    var device = Adapter.ConnectedDevices.FirstOrDefault(d => d.Name == deviceName);

    if (device == null)
        return -1;

    // Vent korrekt på RSSI-opdateringen
    var updated = await device.UpdateRssiAsync();

    if (!updated)
        return -1;

    return device.Rssi;
}



}