using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public abstract class AbstractBluetoothService
{
    private readonly IBluetoothLE _bluetoothLe = CrossBluetoothLE.Current;
    protected readonly IAdapter Adapter = CrossBluetoothLE.Current.Adapter;
    
    private readonly List<MeowDevice> _connectedDevices = new();
    
    private readonly List<MeowDevice> _establishingConnectionDevices = new();

    public event Action<byte[]>? DeviceDataReceived;
    
    public event Action? PeerConnected;
    
    protected AbstractBluetoothService(IErrorService errorService)
    {
        Adapter.DeviceConnected += (_, a) =>
        {
            var device = a.Device;
            if (_connectedDevices.All(cd => cd.Id != device.Id))
            {
                _connectedDevices.Add(new MeowDevice(device.Name ?? string.Empty, device.Id, device));
            }
            _connectedDevices.Add(_establishingConnectionDevices.FirstOrDefault(d => d.Id == device.Id)!);
            _establishingConnectionDevices.RemoveAll(d => d.Id == device.Id);
            PeerConnected?.Invoke();
        };
        
        Adapter.DeviceConnectionLost += (_, a) =>
        {
            var device = a.Device;
            _connectedDevices.RemoveAll(cd => cd.Id == device.Id);
        };
        
        Adapter.DeviceConnectionError += (_, a) =>
        {
            var device = a.Device;
            _connectedDevices.RemoveAll(cd => cd.Id == device.Id);
            _establishingConnectionDevices.RemoveAll(d => d.Id == device.Id);
            errorService.Add(new Exception($"Connection error with device {device.Name}"));
        };

        try
        {
            var currentlyConnected = Adapter.ConnectedDevices;
            if (currentlyConnected != null)
            {
                foreach (var dev in currentlyConnected)
                {
                    if (_connectedDevices.All(cd => cd.Id != dev.Id))
                    {
                        _connectedDevices.Add(new MeowDevice(dev.Name ?? string.Empty, dev.Id, dev));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to populate connected devices: {ex}");
        }
    }

    public async Task<List<MeowDevice>> ScanForDevices()
    {
        if (!_bluetoothLe.IsOn)
            throw new Exception("Bluetooth is off");

        var foundDevices = new List<MeowDevice>();

        Adapter.DeviceDiscovered += (_, a) =>
        {
            if (a.Device.Name != null)
            {
                foundDevices.Add(new MeowDevice(a.Device.Name, a.Device.Id, a.Device));
            }
        };
        await Adapter.StartScanningForDevicesAsync([ChatUuids.ChatService]);
        return foundDevices;
    }


    public async Task ScanAndConnectToAllFoundDevices()
    {
        var devices = await ScanForDevices();
        foreach (var device in devices)
        {
            await ConnectToDevice(device);
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
        return _connectedDevices.ToList();
    }

    public async Task<(bool, List<Exception>)> SendToAllAsync(byte[] data)
    {
        var anySuccess = false;
        var exceptions = new List<Exception>();
        var targets = _connectedDevices.ToList();

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

    public async Task ConnectToDevice(MeowDevice device)
    {
        _establishingConnectionDevices.Add(device);
        if (device.NativeDevice == null)
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
                    characteristic.ValueUpdated += (_, a) =>
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
}