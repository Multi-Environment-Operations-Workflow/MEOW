using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public abstract class AbstractBluetoothService
{
    private readonly IBluetoothLE _bluetoothLe = CrossBluetoothLE.Current;
    protected readonly IAdapter Adapter = CrossBluetoothLE.Current.Adapter;
    private ILoggingService _loggingService;
    private IErrorService _errorService;
    
    public List<MeowDevice> ConnectedDevices { get; } = new();
    
    public List<MeowDevice> EstablishingConnectionDevices { get; } = new();

    private List<MeowDevice> _discoveredDevices = new();
    private DateTime _lastScanTime = DateTime.MinValue;

    public event Action<byte[]>? DeviceDataReceived;
    
    public event Action? PeerConnected;
    
    protected AbstractBluetoothService(IErrorService errorService, ILoggingService loggingService)
    {
        _loggingService = loggingService;
        _errorService = errorService;

        try
        {
            var currentlyConnected = Adapter.ConnectedDevices;
            if (currentlyConnected == null) return;
            
            foreach (var dev in currentlyConnected)
            {
                if (ConnectedDevices.All(cd => cd.Id != dev.Id))
                {
                    ConnectedDevices.Add(new MeowDevice(dev.Name ?? string.Empty, dev.Id, dev));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to populate connected devices: {ex}");
        }
    }
    
    
    public void InvokeDataReceived(byte[] data)
    {
        DeviceDataReceived?.Invoke(data);
    }

    public async Task<List<MeowDevice>> ScanForDevices()
    {
        if (!_bluetoothLe.IsOn)
            throw new Exception("Bluetooth is off");
        
        if (_lastScanTime <= DateTime.Now.AddSeconds(-15) || _discoveredDevices.Count == 0)
        {
            _discoveredDevices.Clear();
            _lastScanTime = DateTime.Now;
        }
        else
        {
            return _discoveredDevices;
        }
        
        Adapter.DeviceDiscovered += (_, a) =>
        {
            _discoveredDevices.Add(new MeowDevice(a.Device.Name, a.Device.Id, a.Device));
        };
        await Adapter.StartScanningForDevicesAsync([ChatUuids.ChatService]);
        return _discoveredDevices;
    }

    public async Task ScanAndConnectToDeviceName(string name)
    {
        var devices = await ScanForDevices();
        var targetDevice = devices.FirstOrDefault(d => d.Name == name);
        if (targetDevice != null)
        {
            await ConnectToDevice(targetDevice);
        }
        else
        {
            throw new Exception($"Device with name {name} not found.");
        }
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
        return ConnectedDevices.ToList();
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
        var targets = ConnectedDevices.ToList();
        
        var adapterThings = Adapter.ConnectedDevices.ToList();

        foreach (var thing in adapterThings)
        {
            _loggingService.AddLog(("Adapter connected device:", thing.Name));
        }

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
        if (EstablishingConnectionDevices.Any(d => d.Id == device.Id))
        {
            throw new Exception("Already establishing connection to device");
        }

        EstablishingConnectionDevices.Add(device);
        if (device.NativeDevice == null)
            throw new ArgumentNullException(nameof(device));

        try
        {
            await Adapter.ConnectToDeviceAsync(device.NativeDevice);
            await device.NativeDevice.RequestMtuAsync(512);
            ConnectedDevices.Add(device);
            PeerConnected?.Invoke();
            _loggingService.AddLog(("Connected to device!", device.NativeDevice));
        }
        catch (Exception exception)
        {
            _errorService.Add(exception);
            return;
        }
        finally
        {
            EstablishingConnectionDevices.RemoveAll(d => d.Id == device.Id);
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