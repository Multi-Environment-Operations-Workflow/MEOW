#if IOS
using System.Collections.ObjectModel;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using CoreBluetooth;
using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public class IOSBluetoothService : NSObject, IBluetoothService, ICBPeripheralManagerDelegate
{
    private readonly IBluetoothLE _bluetooth = CrossBluetoothLE.Current;
    private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
    private readonly IUserStateService _userStateService;

    public ObservableCollection<MeowDevice> Devices { get; } = new();

    private readonly List<MeowDevice> _connectedDevices = new();

    public event Action<byte[]>? DeviceDataReceived;
    
    private CBPeripheralManager? _peripheralManager;

    private CBMutableCharacteristic? _sendCharacteristic;
    private CBMutableCharacteristic? _receiveCharacteristic;

    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;

    public event Action? PeerConnected;

    private readonly CBUUID chatServiceUuid = CBUUID.FromString(ChatUuids.ChatService.ToString());

    public IOSBluetoothService(IUserStateService userStateService)
    {
        _userStateService = userStateService;
        _adapter.DeviceConnected += (s, a) =>
        {
            var device = a.Device;
            if (_connectedDevices.All(cd => cd.Id != device.Id))
            {
                _connectedDevices.Add(new MeowDevice(device.Name ?? string.Empty, device.Id, device));
            }
        };

        try
        {
            var currentlyConnected = _adapter?.ConnectedDevices;
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

    public int GetConnectedDevicesCount()
    {
        return _connectedDevices.Count;
    }

    public async Task<bool> ScanAsync()
    {
        Devices.Clear();
        if (_bluetooth == null || _adapter == null)
            throw new InvalidOperationException("Bluetooth not initialized");
        if (!_bluetooth.IsOn)
            throw new Exception("Bluetooth is off");

        var foundDevices = new List<IDevice>();

        EventHandler<DeviceEventArgs> discoveredHandler = (s, a) =>
        {
            if (foundDevices.Contains(a.Device)) return;
            foundDevices.Add(a.Device);

            if (a.Device.Name != null)
            {
                Devices.Add(new MeowDevice(a.Device.Name, a.Device.Id, a.Device));
            }
        };

        _adapter.DeviceDiscovered += discoveredHandler;
        try
        {
            await _adapter.StartScanningForDevicesAsync();
        }
        finally
        {
            _adapter.DeviceDiscovered -= discoveredHandler;
        }
        
        return true;
    }

    public bool CheckConnection()
    {
        return _adapter.ConnectedDevices.Count > 0;
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

                if (_adapter.ConnectedDevices.All(d => d.Id != native.Id))
                {
                    await _adapter.ConnectToDeviceAsync(native).ConfigureAwait(false);
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
        if (device?.NativeDevice == null)
            throw new ArgumentNullException(nameof(device));

        await _adapter.ConnectToDeviceAsync(device.NativeDevice).ConfigureAwait(false);

        // Optionally, you can enumerate services if you need to initialize something.
        var services = await device.NativeDevice.GetServicesAsync().ConfigureAwait(false);
        foreach (var service in services)
        {
            var characteristics = await service.GetCharacteristicsAsync().ConfigureAwait(false);

            foreach (var characteristic in characteristics)
            {
                // Subscribe only if it's a readable or notifiable characteristic
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


    public async Task StartAdvertisingAsync(string name)
    {
        _peripheralManager = new CBPeripheralManager(this, null);

        // Wait for Bluetooth to be powered on
        while (_peripheralManager.State is CBManagerState.Unknown or CBManagerState.Resetting)
            await Task.Delay(100);

        if (_peripheralManager.State != CBManagerState.PoweredOn)
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth not powered on.");
            return;
        }

        // Create service and characteristics
        var chatService = new CBMutableService(chatServiceUuid, true);

        CBCharacteristicProperties allProps = CBCharacteristicProperties.Read
                                              | CBCharacteristicProperties.Write
                                              | CBCharacteristicProperties.WriteWithoutResponse
                                              | CBCharacteristicProperties.Notify
                                              | CBCharacteristicProperties.Indicate;

        _sendCharacteristic = new CBMutableCharacteristic(
            CBUUID.FromString(ChatUuids.MessageSendCharacteristic.ToString()),
            allProps,
            null,
            CBAttributePermissions.Readable
        );

        _receiveCharacteristic = new CBMutableCharacteristic(
            CBUUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString()),
            allProps,
            null,
            CBAttributePermissions.Writeable
        );

        chatService.Characteristics = new CBCharacteristic[] { _sendCharacteristic, _receiveCharacteristic };

        // Add the service — asynchronous operation
        _peripheralManager.AddService(chatService);
    }

    // Called when service is successfully added
    [Export("peripheralManager:didAddService:error:")]
    public void DidAddService(CBPeripheralManager peripheral, CBService service, NSError error)
    {
        if (error != null)
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, $"Failed to add service: {error.LocalizedDescription}");
            return;
        }

        // Only advertise AFTER the service is added successfully
        var advertiseName = String.Concat("(MEOW) ", _userStateService.GetName());
        var advertisementData = new NSMutableDictionary
        {
            { CBAdvertisement.DataLocalNameKey, new NSString(advertiseName) },
            { CBAdvertisement.DataServiceUUIDsKey, NSArray.FromObjects(service.UUID) }
        };

        peripheral.StartAdvertising(advertisementData);
        AdvertisingStateChanged?.Invoke(AdvertisingState.Started, "Advertising started successfully with service.");
    }


    public async Task StopAdvertisingAsync()
    {
        try
        {
            _peripheralManager?.StopAdvertising();
            AdvertisingStateChanged?.Invoke(AdvertisingState.Stopped, "Advertising stopped.");
        }
        catch (Exception ex)
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, ex.Message);
        }
        await Task.CompletedTask;
    }
    
    // Use mapping when receiving writes
    [Export("peripheralManager:didReceiveWriteRequests:")]
    public void DidReceiveWriteRequests(CBPeripheralManager peripheral, CBATTRequest[] requests)
    {
        foreach (var request in requests)
        {
            if (request.Characteristic?.UUID?.Equals(_receiveCharacteristic?.UUID) == true && request.Value != null)
            {
                var buffer = new byte[request.Value.Length];
                System.Runtime.InteropServices.Marshal.Copy(request.Value.Bytes, buffer, 0, (int)request.Value.Length);
                
                DeviceDataReceived?.Invoke(buffer);

                _peripheralManager?.RespondToRequest(request, CBATTError.Success);
            }
        }
    }

    
    
    [Export("peripheralManagerDidUpdateState:")]
    public void PeripheralManagerDidUpdateState(CBPeripheralManager peripheral)
    {
        switch (peripheral.State)
        {
            case CBManagerState.PoweredOn:
                AdvertisingStateChanged?.Invoke(AdvertisingState.Started, "Bluetooth powered on.");
                break;

            case CBManagerState.PoweredOff:
                AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth powered off.");
                break;

            case CBManagerState.Unauthorized:
                AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth unauthorized.");
                break;

            case CBManagerState.Unsupported:
                AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth unsupported on this device.");
                break;

            default:
                AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, $"Bluetooth state changed: {peripheral.State}");
                break;
        }
    }

}
#endif