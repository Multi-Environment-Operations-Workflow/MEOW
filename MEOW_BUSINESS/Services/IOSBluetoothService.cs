#if IOS
using CoreBluetooth;
using Foundation;
using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;
using ObjCRuntime;

namespace MEOW_BUSINESS.Services;

public class IOSBluetoothService(IUserStateService userStateService, IErrorService errorService, ILoggingService loggingService) : AbstractBluetoothService(errorService, loggingService), IBluetoothService, ICBPeripheralManagerDelegate
{
    private CBPeripheralManager? _peripheralManager;
    private CBMutableCharacteristic? _sendCharacteristic;
    private CBMutableCharacteristic? _receiveCharacteristic;
    
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;

    private readonly CBUUID _chatServiceUuid = CBUUID.FromString(ChatUuids.ChatService.ToString());
    private readonly CBUUID _msgSendUuid = CBUUID.FromString(ChatUuids.MessageSendCharacteristic.ToString());
    private readonly CBUUID _msgRecvUuid = CBUUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString());


    public async Task StartAdvertisingAsync()
    {
        _peripheralManager = new CBPeripheralManager(this, null);

        while (_peripheralManager.State is CBManagerState.Unknown or CBManagerState.Resetting)
            await Task.Delay(100);

        if (_peripheralManager.State != CBManagerState.PoweredOn)
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth not powered on.");
            return;
        }

        var chatService = new CBMutableService(_chatServiceUuid, true);

        _sendCharacteristic = new CBMutableCharacteristic(
            _msgSendUuid,
            CBCharacteristicProperties.Notify | CBCharacteristicProperties.Indicate | CBCharacteristicProperties.Read,
            null,
            CBAttributePermissions.Readable
        );

        _receiveCharacteristic = new CBMutableCharacteristic(
            _msgRecvUuid,
            CBCharacteristicProperties.Write | CBCharacteristicProperties.WriteWithoutResponse | CBCharacteristicProperties.Notify,
            null,
            CBAttributePermissions.Writeable
        );

        chatService.Characteristics = new CBCharacteristic[] { _sendCharacteristic, _receiveCharacteristic };

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

        var advertisementData = new NSMutableDictionary
        {
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
        loggingService.AddLog(("Received write requests via Bluetooth.", null));
        foreach (var request in requests)
        {
            if (request.Characteristic.UUID.Equals(_receiveCharacteristic?.UUID) == true && request.Value != null)
            {
                var buffer = new byte[request.Value.Length];
                System.Runtime.InteropServices.Marshal.Copy(request.Value.Bytes, buffer, 0, (int)request.Value.Length);
                
                InvokeDataReceived(buffer);

                peripheral.RespondToRequest(request, CBATTError.Success);
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