#if IOS
using CoreBluetooth;
using Foundation;
using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;
using ObjCRuntime;

namespace MEOW_BUSINESS.Services;

public class IOSBluetoothService : AbstractBluetoothService, IBluetoothService, ICBPeripheralManagerDelegate
{
    private readonly CBPeripheralManager _peripheralManager;
    public CBMutableCharacteristic? _sendCharacteristic;
    public CBMutableCharacteristic? _receiveCharacteristic;

    private IErrorService _errorService;
    private ILoggingService _loggingService;
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;

    private readonly CBUUID _chatServiceUuid = CBUUID.FromString(ChatUuids.ChatService.ToString());
    private readonly CBUUID _msgSendUuid = CBUUID.FromString(ChatUuids.MessageSendCharacteristic.ToString());
    private readonly CBUUID _msgRecvUuid = CBUUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString());

    public IOSBluetoothService(IErrorService errorService, ILoggingService loggingService) : base(errorService,
    {
        _peripheralManager = new(this, null);
        _errorService = errorService;
        _loggingService = loggingService;

        _peripheralManager.WriteRequestsReceived += WriteRequestReceived;
        _peripheralManager.StateUpdated += PeripheralManagerDidUpdateState;
    }

    public async Task StartAdvertisingAsync()
    {
        try
        {
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
                CBCharacteristicProperties.Notify | CBCharacteristicProperties.Indicate |
                CBCharacteristicProperties.Read,
                null,
                CBAttributePermissions.Readable
            );

            _receiveCharacteristic = new CBMutableCharacteristic(
                _msgRecvUuid,
                CBCharacteristicProperties.Write | CBCharacteristicProperties.WriteWithoutResponse |
                CBCharacteristicProperties.Notify,
                null,
                CBAttributePermissions.Writeable
            );

            chatService.Characteristics = new CBCharacteristic[] { _sendCharacteristic, _receiveCharacteristic };

            _peripheralManager.AddService(chatService);
            var advertisementData = new NSMutableDictionary
            {
                { CBAdvertisement.DataServiceUUIDsKey, NSArray.FromObjects(chatService.UUID) }
            };

            _peripheralManager.StartAdvertising(advertisementData);
        }
        catch (Exception ex)
        {
            _errorService.Add(ex);
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, ex.Message);
        }
    }

    public async Task StopAdvertisingAsync()
    {
        try
        {
            _peripheralManager.StopAdvertising();
            AdvertisingStateChanged?.Invoke(AdvertisingState.Stopped, "Advertising stopped.");
        }
        catch (Exception ex)
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, ex.Message);
        }

        await Task.CompletedTask;
    }

    private void WriteRequestReceived(object? sender, CBATTRequestsEventArgs e)
    {
        try
        {
            var request = e.Requests.FirstOrDefault(r => r.Characteristic.UUID.Equals(_msgRecvUuid));
            if (request == null || request.Value == null)
            {
                _errorService.Add(new Exception("No valid write request found."));
                return;
            }
            
            var buffer = new byte[request.Value.Length];
            System.Runtime.InteropServices.Marshal.Copy(
                request.Value.Bytes,
                buffer,
                0,
                (int)request.Value.Length
            );

            _loggingService.AddLog(("Received Bluetooth message:",
                System.Text.Encoding.UTF8.GetString(buffer)));
            
            InvokeDataReceived(buffer);

            if (sender is CBPeripheralManager peripheral)
            {
                peripheral.RespondToRequest(request, CBATTError.Success);
            }
        }
        catch (Exception ex)
        {
            _errorService.Add(ex);
        }
    }

    private void PeripheralManagerDidUpdateState(object? peripheral, EventArgs e)
    {
        if (peripheral is not CBPeripheralManager peripheralManager)
        {
            _loggingService.AddLog(("PeripheralManagerDidUpdateState called with invalid peripheral.", peripheral));
            return;
        }

        switch (peripheralManager.State)
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
                AdvertisingStateChanged?.Invoke(AdvertisingState.Failed,
                    $"Bluetooth state changed: {peripheralManager.State}");
                break;
        }
    }

    public NativeHandle Handle { get; }

    public void Dispose()
    {
        _peripheralManager.Dispose();
    }
}

#endif