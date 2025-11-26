using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using MEOW_BUSINESS.Models;
using Plugin.BLE.Abstractions;

namespace MEOW_BUSINESS.Services;

public abstract class AbstractBluetoothService
{
    private readonly IBluetoothLE _bluetoothLe = CrossBluetoothLE.Current;
    protected readonly IAdapter Adapter = CrossBluetoothLE.Current.Adapter;
    private ILoggingService _loggingService;
    private IErrorService _errorService;
    
    private readonly List<MeowDevice> _connectedDevices = new();
    
    private readonly List<MeowDevice> _establishingConnectionDevices = new();

    public event Action<byte[]>? DeviceDataReceived;
    
    public event Action? PeerConnected;
    
    protected AbstractBluetoothService(IErrorService errorService, ILoggingService loggingService)
    {
        _loggingService = loggingService;
        _errorService = errorService;
        Adapter.DeviceConnected += (_, a) =>
        {
            var device = a.Device;
            if (_connectedDevices.All(cd => cd.Id != device.Id))
            {
                _connectedDevices.Add(new MeowDevice(device.Name ?? string.Empty, device.Id, device));
            }
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
            if (currentlyConnected == null) return;
            
            foreach (var dev in currentlyConnected)
            {
                if (_connectedDevices.All(cd => cd.Id != dev.Id))
                {
                    _connectedDevices.Add(new MeowDevice(dev.Name ?? string.Empty, dev.Id, dev));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to populate connected devices: {ex}");
        }
    }
    
    
    protected void InvokeDataReceived(byte[] data)
    {
        DeviceDataReceived?.Invoke(data);
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
        await Adapter.StartScanningForDevicesAsync();
        var filteredDevices = new List<MeowDevice>(); // replace DeviceType with your type
        foreach (var device in foundDevices)
        {
            try
            {
                var service = await device.NativeDevice.GetServiceAsync(ChatUuids.ChatService);
                if (service != null)
                    filteredDevices.Add(device);
            }
            catch
            {
                // Service not found, ignore this device
            }
        }

        return filteredDevices.Count > 0 ? filteredDevices : foundDevices;
    }


    public async Task ScanAndConnectToAllFoundDevices()
    {
        var devices = await ScanForDevices();
        foreach (var device in devices)
        {
            await ConnectToDevice(device);
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
    
    /// <summary>
    /// Sends data to a specific device.
    /// </summary>
    /// <param name="device"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="Exception">If the native device, service, or characteristic is not found.</exception>
    public async Task SendToDevice(MeowDevice device, byte[] data)
    {
        var native = device.NativeDevice;
            
        if (native == null)
            throw new Exception($"Native device is null for {device.Name}");

        var service = await native.GetServiceAsync(ChatUuids.ChatService);
            
        if (service == null)
        {
            throw new Exception($"Service {ChatUuids.ChatService} not found on {device.Name}.");
        }

        var sendCharacteristic = await service.GetCharacteristicAsync(Chatuuids.MessageSendCharacteristic);
        if (sendCharacteristic == null)
        {
            throw new Exception($"Message Send Characteristic not found on {device.Name}.");
        }

        if (messageSendChar.Properties.HasFlag(CharacteristicPropertyType.Notify))
        {
            messageSendChar.ValueUpdated += (s, e) =>
            {
                DeviceDataReceived?.Invoke(e.Characteristic.Value);
            };
            await messageSendChar.StartUpdatesAsync();
        }
            
        var receiveCharacteristic = await service.GetCharacteristicAsync(ChatUuids.MessageReceiveCharacteristic);
        if (receiveCharacteristic == null) 
        {
            throw new Exception($"Characteristic {ChatUuids.MessageReceiveCharacteristic} not found on {device.Name}.");
        }
        _loggingService.AddLog(("Sending data to device:", device.Name));
        await receiveCharacteristic.WriteAsync(data);
    }

    public async Task<(bool, List<Exception>)> BroadcastMessage(byte[] data)
    {
        var anySuccess = false;
        var targets = _connectedDevices.ToList();

        foreach (var device in targets)
        {
            try
            {
                _loggingService.AddLog(("Broadcasting message to device:", device.Name));
                await SendToDevice(device, data);
                anySuccess = true;
            }
            catch (Exception ex)
            {
                _errorService.Add(ex);
            }
        }

        return (anySuccess, null);
    }

    public async Task ConnectToDevice(MeowDevice device)
    {
        if (_establishingConnectionDevices.Any(d => d.Id == device.Id))
        {
            throw new Exception("Already establishing connection to device");
        }

        _establishingConnectionDevices.Add(device);
        if (device.NativeDevice == null)
            throw new ArgumentNullException(nameof(device));

        try
        {
            await Adapter.ConnectToDeviceAsync(device.NativeDevice);
            _loggingService.AddLog(("Connected to device!", device.NativeDevice));
        }
        catch (Exception exception)
        {
            _errorService.Add(exception);
            return;
        }
        finally
        {
            _establishingConnectionDevices.RemoveAll(d => d.Id == device.Id);
        }


        var service = await device.NativeDevice.GetServiceAsync(ChatUuids.ChatService);
        _loggingService.AddLog(("Device has service:", service.Id));

        if (service == null)
        {
            throw new Exception("Service not found on device after connection.");
        }
        
        var receiveCharacteristic = await service.GetCharacteristicAsync(ChatUuids.MessageReceiveCharacteristic);
        _loggingService.AddLog(("Device has char:", receiveCharacteristic.Id));

        if (receiveCharacteristic == null)
        {
            throw new Exception("Characteristic not found on device after connection.");
        }
        
        await receiveCharacteristic.StartUpdatesAsync();
    }
}