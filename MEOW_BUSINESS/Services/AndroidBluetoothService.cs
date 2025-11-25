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
    public new event Action<AdvertisingState, string?>? AdvertisingStateChanged;

    public new event Action? PeerConnected;

    public new event Action<byte[]>? DeviceDataReceived;

    private BluetoothLeAdvertiser? _bleAdvertiser;

    private AdvertisingCallback? _advertisingCallback;

    private static BluetoothManager _bluetoothManager;
    private BluetoothGattServer _gattServer;

    private readonly UUID _chatServiceUuid = UUID.FromString(ChatUuids.ChatService.ToString())!;
    private readonly UUID _msgSendUuid = UUID.FromString(ChatUuids.MessageSendCharacteristic.ToString())!;
    private readonly UUID _msgRecvUuid = UUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString())!;

    private IErrorService _errorService;

    // For at ungå dupes
    private bool _isAdvertising = false;
    private bool _isScanning = false;

    public AndroidBluetoothService(IErrorService errorService) : base(errorService)
    {
        _errorService = errorService;
        _bluetoothManager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
        MeowGattCallback callback = new(OnReceive);
        _gattServer = _bluetoothManager.OpenGattServer(Android.App.Application.Context, callback);

        callback.SetGattServer(_gattServer);
    }

    private void OnReceive(byte[] data)
    {
        DeviceDataReceived.Invoke(data);
    }

    // IBluetoothService.StartAdvertisingAsync
    public async Task StartAdvertisingAsync(string name)
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
            GattProperty.Write | GattProperty.WriteNoResponse,
            GattPermission.Write);

        service.AddCharacteristic(sendChar);
        service.AddCharacteristic(recvChar);

        _gattServer.AddService(service);

        // 2) Advertising med service UUID = ChatUuids.ChatService
        // Mere opsætning til advertising
        var adapter = _bluetoothManager?.Adapter;
        var advertiser = adapter?.BluetoothLeAdvertiser;

        if (advertiser == null)
        {
            AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth not powered on or not supported.");
            return;
        }

        // Mere settings
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
            onSuccess: () => AdvertisingStateChanged?.Invoke(AdvertisingState.Started, $"Advertising as {name}"),
            onFailure: (errorMessage) => AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, errorMessage));
        // Her starter vi så faktisk det som gør vi kan se enheden på andre enheder.
        advertiser.StartAdvertising(settings, data, callback);
        await Task.CompletedTask;
    }

    private void OnDeviceDiscovered(object? s, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs a)
    { // Tjekker om vi allerede har den. Vis vi har gør vi ikke noget. Ellers tilføjer vi den til vores liste.
        if (DiscoveredDevices.Any(d => d.Id == a.Device.Id))
            return;

        if (a.Device.Name != null)
        {
            var device = new MeowDevice(a.Device.Name, a.Device.Id, a.Device);
            device.Name = device.Name.Replace("(MEOW) ", "").Trim();
            DiscoveredDevices.Add(device);
        }
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
                    Adapter.DeviceDiscovered -= OnDeviceDiscovered; // Fjerner handles fordi vi også stopper scanning.  
                }
            }

            // Vis ikke vi cleare den risikere vi den går med over. Måske. Skal jeg lige teste !TODO
            DiscoveredDevices.Clear();

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

            //_gattServer?.Stop();
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
            Adapter.DeviceDiscovered -= OnDeviceDiscovered;
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

    private readonly HashSet<BluetoothDevice> _subscribers = new();

    public void SetGattServer(BluetoothGattServer server)
    {
        _gattServer = server;
    }

    public override void OnConnectionStateChange(BluetoothDevice? device, ProfileState status, ProfileState newState)
    {
        if (newState == ProfileState.Connected) _subscribers.Add(device);
        else _subscribers.Remove(device);
    }

    public override void OnCharacteristicReadRequest(BluetoothDevice? device, int requestId, int offset, BluetoothGattCharacteristic? characteristic)
    {
        var bytes = characteristic.GetValue() ?? Array.Empty<byte>();
        _gattServer.SendResponse(device, requestId, GattStatus.Success, offset, bytes);
    }

    public override void OnCharacteristicWriteRequest(
        BluetoothDevice? device, int requestId, BluetoothGattCharacteristic? characteristic,
        bool preparedWrite, bool responseNeeded, int offset, byte[]? value)
    {
        if (characteristic.Uuid.Equals(messageReceiveUuid) && value != null)
            onReceive.Invoke(value);

        var resp = value ?? Array.Empty<byte>();
        if (responseNeeded)
            _gattServer.SendResponse(device, requestId, GattStatus.Success, offset, resp);
    }
}

#endif
