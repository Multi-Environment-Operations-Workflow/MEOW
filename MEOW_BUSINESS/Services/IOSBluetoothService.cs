#if IOS
using CoreBluetooth;
using Foundation;
using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;
using ObjCRuntime;

namespace MEOW_BUSINESS.Services;

public class IOSBluetoothService(IUserStateService userStateService, IErrorService errorService) : AbstractBluetoothService(errorService), IBluetoothService, ICBPeripheralManagerDelegate
{
    private CBPeripheralManager? _peripheralManager;
    private CBMutableCharacteristic? _sendCharacteristic;
    private CBMutableCharacteristic? _receiveCharacteristic;
    
    public new event Action<byte[]>? DeviceDataReceived;

    public new event Action<AdvertisingState, string?>? AdvertisingStateChanged;

    private readonly CBUUID _chatServiceUuid = CBUUID.FromString(ChatUuids.ChatService.ToString());
    private readonly CBUUID _msgSendUuid = CBUUID.FromString(ChatUuids.MessageSendCharacteristic.ToString());
    private readonly CBUUID _msgRecvUuid = CBUUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString());


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
        var chatService = new CBMutableService(_chatServiceUuid, true);

        CBCharacteristicProperties allProps = CBCharacteristicProperties.Read
                                              | CBCharacteristicProperties.Write
                                              | CBCharacteristicProperties.WriteWithoutResponse
                                              | CBCharacteristicProperties.Notify
                                              | CBCharacteristicProperties.Indicate;

        _sendCharacteristic = new CBMutableCharacteristic(
            _msgSendUuid,
            allProps,
            null,
            CBAttributePermissions.Readable
        );

        _receiveCharacteristic = new CBMutableCharacteristic(
            _msgRecvUuid,
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
        var advertisementData = new NSMutableDictionary
        {
            { CBAdvertisement.DataLocalNameKey, new NSString(userStateService.GetName()) },
            { CBAdvertisement.DataServiceUUIDsKey, service.UUID }
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
            if (request.Characteristic.UUID.Equals(_receiveCharacteristic?.UUID) == true && request.Value != null)
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

    public void Dispose()
    {
        _peripheralManager?.Dispose();
        _sendCharacteristic?.Dispose();
        _receiveCharacteristic?.Dispose();
        _chatServiceUuid.Dispose();
        _msgSendUuid.Dispose();
        _msgRecvUuid.Dispose();
    }

    public NativeHandle Handle { get; }
}
#endif