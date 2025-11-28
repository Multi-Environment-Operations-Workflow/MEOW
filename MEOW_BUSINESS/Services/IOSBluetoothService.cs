#if IOS
using CoreBluetooth;
using Foundation;
using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;

namespace MEOW_BUSINESS.Services;

public class IOSBluetoothService(IErrorService errorService, ILoggingService loggingService) : AbstractBluetoothService(errorService, loggingService), IBluetoothService
{
    private CBPeripheralManager? _peripheralManager;
    public CBMutableCharacteristic? _sendCharacteristic;
    public CBMutableCharacteristic? _receiveCharacteristic;
    
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;

    private readonly CBUUID _chatServiceUuid = CBUUID.FromString(ChatUuids.ChatService.ToString());
    private readonly CBUUID _msgSendUuid = CBUUID.FromString(ChatUuids.MessageSendCharacteristic.ToString());
    private readonly CBUUID _msgRecvUuid = CBUUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString());


    public async Task StartAdvertisingAsync()
    {
        _peripheralManager = new CBPeripheralManager(new IOSGyatt(loggingService, this, errorService), null);

        while (_peripheralManager.State is CBManagerState.Unknown or CBManagerState.Resetting)
            await Task.Delay(100);

        if (_peripheralManager.State != CBManagerState.PoweredOn)
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth not powered on.");
            return;
        }

        var chatService = new CBMutableService(_chatServiceUuid, true);
        loggingService.AddLog(("Created chat service for Bluetooth advertising.", _chatServiceUuid));
        loggingService.AddLog(("Standard UUID:", ChatUuids.ChatService));

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
}

class IOSGyatt(ILoggingService loggingService, IOSBluetoothService iosBluetoothService, IErrorService errorService) : CBPeripheralManagerDelegate
{
    
    private readonly CBUUID _chatServiceUuid = CBUUID.FromString(ChatUuids.ChatService.ToString());
    private readonly CBUUID _msgSendUuid = CBUUID.FromString(ChatUuids.MessageSendCharacteristic.ToString());
    private readonly CBUUID _msgRecvUuid = CBUUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString());
    
    [Export("peripheralManager:didAddService:error:")]
    public void DidAddService(CBPeripheralManager peripheral, CBService service, NSError error)
    {
        try
        {
            if (error != null)
            {
                loggingService.AddLog(("Failed to add Bluetooth service.", error));
                return;
            }

            var advertisementData = new NSMutableDictionary
            {
                { CBAdvertisement.DataServiceUUIDsKey, NSArray.FromObjects(service.UUID) }
            };

            peripheral.StartAdvertising(advertisementData);
            loggingService.AddLog(("Started advertising Bluetooth service.", service.UUID));
        }
        catch (Exception ex)
        {
            errorService.Add(ex);
        }
    }
    
    // Use mapping when receiving writes
    [Export("peripheralManager:didReceiveWriteRequests:")]
    public void DidReceiveWriteRequests(CBPeripheralManager peripheral, CBATTRequest[] requests)
    {
        try
        {
            loggingService.AddLog(("Received write requests via Bluetooth.", null));
            foreach (var request in requests)
            {
                if (request.Characteristic.UUID.Equals(_msgSendUuid) && request.Value != null)
                {
                    var buffer = new byte[request.Value.Length];
                    System.Runtime.InteropServices.Marshal.Copy(request.Value.Bytes, buffer, 0,
                        (int)request.Value.Length);

                    iosBluetoothService.InvokeDataReceived(buffer);

                    peripheral.RespondToRequest(request, CBATTError.Success);
                }
            }
        }
        catch (Exception ex)
        {
            errorService.Add(ex);
        }
    }

    
    
    /**[Export("peripheralManagerDidUpdateState:")]
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
    }*/
}
#endif