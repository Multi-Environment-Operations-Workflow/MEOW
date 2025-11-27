#if ANDROID
using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Models;
using Android.Bluetooth.LE;
using Android.Bluetooth;
using Android.Content;
using Java.Util;

namespace MEOW_BUSINESS.Services;

/// <summary>
/// Implementering af Bluetooth-service til Android-platformen.
/// Det er lavet til at skulle:
///     - Søge efter andre enheder (ScanAsync)
///     - Oprette forbindelse til en enhed (ConnectAsync)
///     - Starte advertising som en Bluetooth-peripheral (StartAdvertisingAsync)
///     - Stoppe advertising (StopAdvertisingAsync)
///     - Sende data til alle forbundne enheder (SendToAllAsync
///     - Håndtere indkommende data fra forbundne enheder (DeviceDataReceived event)
///     - Retunere antal forbundne enheder (GetConnectedDevicesCount)
///     - Spørger om tilladelser (CheckPermissions)
/// 
/// </summary>
public class AndroidBluetoothService : AbstractBluetoothService, IBluetoothService // Implementering af IBluetoothService til Android
{
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;

    public new event Action? PeerConnected;
    
    private BluetoothLeAdvertiser? _bleAdvertiser;

    private AdvertisingCallback? _advertisingCallback;

    private static BluetoothManager _bluetoothManager;
    private BluetoothGattServer _gattServer;

    private readonly UUID _chatServiceUuid = UUID.FromString(ChatUuids.ChatService.ToString())!;
    private readonly UUID _msgSendUuid = UUID.FromString(ChatUuids.MessageSendCharacteristic.ToString())!;
    private readonly UUID _msgRecvUuid = UUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString())!;

    private IErrorService _errorService;
    private ILoggingService _loggingService;

    private bool _isAdvertising = false;
    private bool _isScanning = false;

    public AndroidBluetoothService(IErrorService errorService, ILoggingService loggingService) : base(errorService, loggingService)
    {
        _errorService = errorService;
        _loggingService = loggingService;
        _bluetoothManager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
        MeowGattCallback callback = new(InvokeDataReceived);
        _gattServer = _bluetoothManager.OpenGattServer(Android.App.Application.Context, callback);

        callback.SetGattServer(_gattServer, loggingService, this);
    }

    public async Task StartAdvertisingAsync()
    {
        if (_isAdvertising)
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Already advertising");
            return;
        }
        _isAdvertising = true;

        if (!await CheckPermissions())
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth permissions not granted.");
            return;
        }

        var service = new BluetoothGattService(_chatServiceUuid, GattServiceType.Primary);

        var sendChar = new BluetoothGattCharacteristic(
            _msgSendUuid,
            GattProperty.Read | GattProperty.Notify,
            GattPermission.Read);

        var recvChar = new BluetoothGattCharacteristic(
            _msgRecvUuid,
            GattProperty.Write | GattProperty.WriteNoResponse | GattProperty.Notify,
            GattPermission.Write);

        service.AddCharacteristic(sendChar);
        service.AddCharacteristic(recvChar);

        _gattServer.AddService(service);
        
        var adapter = _bluetoothManager?.Adapter;
        var advertiser = adapter?.BluetoothLeAdvertiser;

        if (advertiser == null)
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth not powered on or not supported.");
            return;
        }

        var settings = new AdvertiseSettings.Builder()
            .SetAdvertiseMode(AdvertiseMode.Balanced)
            .SetConnectable(true)
            .SetTimeout(0)
            .SetTxPowerLevel(AdvertiseTx.PowerMedium)// måske burde vi sætte til high, giver bedre rækkevidde
            .Build();

        var data = new AdvertiseData.Builder()
            .AddServiceUuid(Android.OS.ParcelUuid.FromString(ChatUuids.ChatService.ToString()))
            .SetIncludeDeviceName(true)
            .Build();

        // Callback til at håndtere advertising resultater
        var callback = new AdvertisingCallback(
            onSuccess: () => AdvertisingStateChanged?.Invoke(AdvertisingState.Started, $"Advertising as {adapter?.Name}"),
            onFailure: (errorMessage) => AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, errorMessage));
        // Her starter vi så faktisk det som gør vi kan se enheden på andre enheder.
        advertiser.StartAdvertising(settings, data, callback);
        await Task.CompletedTask;
    }

    public Task StopAdvertisingAsync()
    {
        try
        {
            //  Stop aktiv scanning, hvis en kører. Virker delvist
            if (_isScanning)
            {
                try
                {
                    if (Adapter.IsScanning)
                        Adapter.StopScanningForDevicesAsync();
                }
                catch (Exception ex)
                {
                    _errorService.Add(ex);
                }
                finally
                {
                    _isScanning = false;
                }
            }

            if (_bleAdvertiser != null && _advertisingCallback != null)
            {
                try
                {
                    _bleAdvertiser.StopAdvertising(_advertisingCallback);
                }
                catch (Exception ex)
                {
                    _errorService.Add(ex);
                }
            }

            _gattServer = null;

            _advertisingCallback = null;
            _isAdvertising = false;

            AdvertisingStateChanged?.Invoke(AdvertisingState.Stopped, "Advertising stopped");
        }
        catch (Exception ex)
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, $"Failed to stop advertising: {ex.Message}"); // kan fx ske vis den ikke advetizer til at starte med.
            _isAdvertising = false;
            _advertisingCallback = null;
            _isScanning = false;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks and requests Bluetooth permissions on Android.
    /// </summary>
    async Task<bool> CheckPermissions()
    {
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();

        switch (status)
        {
            case PermissionStatus.Granted:
                return true;
            case PermissionStatus.Denied:
                {
                    if (Permissions.ShouldShowRationale<Permissions.Bluetooth>())
                    {
                        var page = Application.Current?.Windows[0]?.Page;
                        if (page != null)
                        {
                            await page.DisplayAlert(
                                "Bluetooth Access Required",
                                "This app needs access to bluetooth to function",
                                "OK"
                            );
                        }
                    }
                    break;
                }
        }

        status = await Permissions.RequestAsync<Permissions.Bluetooth>();
        return status == PermissionStatus.Granted;
    }
}

class AdvertisingCallback(Action onSuccess, Action<string> onFailure) : AdvertiseCallback
{
    public override void OnStartSuccess(AdvertiseSettings? settingsInEffect) => onSuccess();
    public override void OnStartFailure(AdvertiseFailure errorCode)
    {
        base.OnStartFailure(errorCode);

        var errorMessage = errorCode switch
        {
            AdvertiseFailure.DataTooLarge => "Advertising data too large",
            AdvertiseFailure.TooManyAdvertisers => "Too many advertisers",
            AdvertiseFailure.AlreadyStarted => "Advertising already started",
            AdvertiseFailure.InternalError => "Internal error",
            AdvertiseFailure.FeatureUnsupported => "Feature unsupported",
            _ => $"Advertising failed with code: {errorCode}"
        };

        onFailure?.Invoke(errorMessage);
    }
}

class MeowGattCallback(Action<byte[]> onReceive) : BluetoothGattServerCallback
{
    private readonly UUID messageReceiveUuid = UUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString())!;

    private BluetoothGattServer? _gattServer;
    
    private ILoggingService _loggingService;
    
    private IBluetoothService _bluetoothService;

    public void SetGattServer(BluetoothGattServer server, ILoggingService loggingService, IBluetoothService bluetoothService)
    {
        _gattServer = server;
        _loggingService = loggingService;
        _bluetoothService = bluetoothService;
    }

    public override void OnCharacteristicReadRequest(BluetoothDevice? device, int requestId, int offset, BluetoothGattCharacteristic? characteristic)
    {
        var bytes = characteristic.GetValue() ?? Array.Empty<byte>();
        _gattServer.SendResponse(device, requestId, GattStatus.Success, offset, bytes);
    }

    public override void OnConnectionStateChange(BluetoothDevice? device, ProfileState status, ProfileState newState)
    {
        _loggingService.AddLog(("Device id: " + device?.Name, null));
        try
        {
            if (String.IsNullOrEmpty(device?.Name))
            {
                return;  
            }
            if (_bluetoothService.EstablishingConnectionDevices.Any(d => d.Name == device.Name))
            {
                return;
            }
            if (_bluetoothService.ConnectedDevices.Any(d => d.Name == device.Name))
            {
                return;
            }
            _bluetoothService.ScanAndConnectToDeviceName(device.Name);
        }
        catch (Exception ex)
        {
            _loggingService.AddLog(("Error connecting to device: " + ex.Message, device));
        }
    }

    public override void OnCharacteristicWriteRequest(
        BluetoothDevice? device, int requestId, BluetoothGattCharacteristic? characteristic,
        bool preparedWrite, bool responseNeeded, int offset, byte[]? value)
    {
        onReceive.Invoke(value);

        var resp = value ?? Array.Empty<byte>();
        if (responseNeeded)
            _gattServer.SendResponse(device, requestId, GattStatus.Success, offset, resp);
    }
}

#endif
