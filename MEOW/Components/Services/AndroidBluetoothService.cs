#if ANDROID
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using MEOW.Components.Enums;
using MEOW.Components.Models;
using Android.Bluetooth.LE;
using Android.Bluetooth;
using Android.Content;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions;
using Java.Util;

namespace MEOW.Components.Services
{
    public class AndroidBluetoothService : IBluetoothService // Implementering af IBluetoothService til Android
    {
        private readonly IBluetoothLE _bluetooth = CrossBluetoothLE.Current;
        private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;

        public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
        public event Action? PeerConnected;
        public event Action<byte[]>? DeviceDataReceived;

        public ObservableCollection<MeowDevice> Devices { get; } = new();
        private BluetoothLeAdvertiser? _bleAdvertiser;
        private AdvertisingCallback? _advertisingCallback;

        // NYT: lokal GATT-server så Android kan modtage beskeder
        private MeowAndroidGattServer? _gattServer;

        // IBluetoothService.GetConnectedDevicesCount
        public int GetConnectedDevicesCount()
        {
            return _adapter.ConnectedDevices?.Count ?? 0;
        }

        // IBluetoothService.SendToAllAsync(byte[])
        public async Task<(bool, List<Exception>)> SendToAllAsync(byte[] data)
        {
            var anySuccess = false;
            var exceptions = new List<Exception>();

            // A) Send som CENTRAL til allerede forbundne enheder (Plugin.BLE)
            foreach (var device in _adapter.ConnectedDevices.ToList())
            {
                try
                {
                    try { await device.RequestMtuAsync(185).ConfigureAwait(false); } catch { /* best effort */ }

                    var services = await device.GetServicesAsync().ConfigureAwait(false);
                    var chatService = services.FirstOrDefault(s => s.Id == ChatUuids.ChatService);

                    if (chatService == null)
                        throw new Exception($"Service {ChatUuids.ChatService} not found on {device.Name}");

                    var characteristics = await chatService.GetCharacteristicsAsync().ConfigureAwait(false);
                    var messageReceiveChar = characteristics.FirstOrDefault(c => c.Id == ChatUuids.MessageReceiveCharacteristic);

                    if (messageReceiveChar == null)
                        throw new Exception($"Characteristic {ChatUuids.MessageReceiveCharacteristic} not found on {device.Name}");

                    // Plugin.BLE: brug WriteType + WriteAsync(data)
                    if (messageReceiveChar.Properties.HasFlag(CharacteristicPropertyType.Write))
                    {
                        messageReceiveChar.WriteType = CharacteristicWriteType.WithResponse;
                        await messageReceiveChar.WriteAsync(data).ConfigureAwait(false);
                    }
                    else if (messageReceiveChar.Properties.HasFlag(CharacteristicPropertyType.WriteWithoutResponse))
                    {
                        messageReceiveChar.WriteType = CharacteristicWriteType.WithoutResponse;
                        await messageReceiveChar.WriteAsync(data).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new Exception("Characteristic is not writable");
                    }

                    anySuccess = true;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            // B) Notify centraler der er forbundet til VORES Android-peripheral (GATT-serveren)
            try
            {
                if (_gattServer?.NotifyAll(data) == true)
                    anySuccess = true;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            return (anySuccess, exceptions);
        }

        // IBluetoothService.StartAdvertisingAsync
        public async Task StartAdvertisingAsync(string name)
        {
            // Start GATT-server først (så write-requests kan modtages)
            if (!await CheckPermissions())
            {
                AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth permissions not granted.");
                return;
            }

            // 1) GATT-server (peripheral) – samme service/char UUIDs som iOS
            _gattServer = new MeowAndroidGattServer(Android.App.Application.Context);
            _gattServer.MessageReceived += bytes => DeviceDataReceived?.Invoke(bytes);
            _gattServer.Start();

            // 2) Advertising med service UUID = ChatUuids.ChatService
            var bluetoothManager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
            var adapter = bluetoothManager?.Adapter;
            var advertiser = adapter?.BluetoothLeAdvertiser;

            if (advertiser == null)
            {
                AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth not powered on or not supported.");
                return;
            }

            adapter?.SetName($"(MEOW) {name}");

            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.Balanced)
                .SetConnectable(true)
                .SetTimeout(0)
                .SetTxPowerLevel(AdvertiseTx.PowerMedium)
                .Build();

            var data = new AdvertiseData.Builder()
                .AddServiceUuid(Android.OS.ParcelUuid.FromString(ChatUuids.ChatService.ToString()))
                .SetIncludeDeviceName(true)
                .Build();

            var callback = new AdvertisingCallback(
                onSuccess: () => AdvertisingStateChanged?.Invoke(AdvertisingState.Started, $"Advertising as {name}"),
                onFailure: (errorMessage) => AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, errorMessage));

            advertiser.StartAdvertising(settings, data, callback);
            await Task.CompletedTask;
        }

        // Overload med custom serviceUuid — samme logik, bare bruger den UUID
        public async Task StartAdvertisingAsync(string name, Guid serviceUuid)
        {
            if (!await CheckPermissions())
                return;

            // 1) GATT-server (serviceUuid bruges her)
            _gattServer = new MeowAndroidGattServer(Android.App.Application.Context, serviceUuid);
            _gattServer.MessageReceived += bytes => DeviceDataReceived?.Invoke(bytes);
            _gattServer.Start();

            // 2) Advertising
            var _bluetoothManager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
            _bleAdvertiser = _bluetoothManager?.Adapter?.BluetoothLeAdvertiser;
            var adapter = _bluetoothManager?.Adapter;

            adapter?.SetName($"{name}");

            if (_bleAdvertiser == null)
            {
                AdvertisingStateChanged?.Invoke(AdvertisingState.Started, "Bluetooth not powered on or not supported.");
                return;
            }

            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.Balanced)
                .SetConnectable(true)
                .SetTimeout(0)
                .SetTxPowerLevel(AdvertiseTx.PowerMedium)
                .Build();

            var data = new AdvertiseData.Builder()
                .AddServiceUuid(Android.OS.ParcelUuid.FromString(serviceUuid.ToString()))
                .Build();

            var scanResponse = new AdvertiseData.Builder()
                .SetIncludeDeviceName(true)
                .Build();

            _advertisingCallback = new AdvertisingCallback(
                onSuccess: () => AdvertisingStateChanged?.Invoke(AdvertisingState.Started, $"Advertising as {name}"),
                onFailure: (errorMessage) => AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, errorMessage));

            _bleAdvertiser.StartAdvertising(settings, data, scanResponse, _advertisingCallback);
        }

        public async Task<bool> ScanAsync()
        {
            await CheckPermissions();

            Devices.Clear();
            if (_bluetooth == null || _adapter == null)
                throw new InvalidOperationException("Bluetooth not initialized");

            if (!_bluetooth.IsOn)
                throw new Exception("Bluetooth is off");

            var foundDevices = new List<IDevice>();
            _adapter.DeviceDiscovered += (s, a) =>
            {
                if (!foundDevices.Contains(a.Device))
                {
                    foundDevices.Add(a.Device);
                    if (a.Device.Name != null)
                    {
                        var device = new MeowDevice(a.Device.Name, a.Device.Id, a.Device);
                        device.Name = device.Name.Replace("(MEOW) ", "").Trim();
                        Devices.Add(device);
                    }
                }
            };
            await _adapter.StartScanningForDevicesAsync();
            return true;
        }

        async public Task ConnectAsync(MeowDevice device)
        {
            try
            {
                // Brug NativeDevice hvis vi har den (mest robust)
                if (device?.NativeDevice is IDevice native)
                {
                    await _adapter.ConnectToDeviceAsync(native);
                    try { await native.RequestMtuAsync(185); } catch { /* best effort */ }
                }
                else
                {
                    await _adapter.ConnectToKnownDeviceAsync(device.Id);
                }

                PeerConnected?.Invoke();
            }
            catch (DeviceConnectionException ex)
            {
                throw new Exception($"Failed to connect to device: {ex.Message}");
            }
        }

        public Task StopAdvertisingAsync()
        {
            try
            {
                _bleAdvertiser?.StopAdvertising(_advertisingCallback);
                _gattServer?.Stop();
                _gattServer = null;

                AdvertisingStateChanged?.Invoke(AdvertisingState.Stopped, "Advertising stopped");
            }
            catch (Exception ex)
            {
                AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, $"Failed to stop advertising: {ex.Message}");
            }

            _advertisingCallback = null;

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

    class AdvertisingCallback : AdvertiseCallback
    {
        private readonly Action _onSuccess;
        private readonly Action<string> _onFailure;

        public AdvertisingCallback(Action onSuccess, Action<string> onFailure)
        {
            _onSuccess = onSuccess;
            _onFailure = onFailure;
        }

        public override void OnStartSuccess(AdvertiseSettings? settingsInEffect) => _onSuccess();
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

            _onFailure?.Invoke(errorMessage);
        }
    }

    // ============================================================
    // LILLE INTERN GATT-SERVER
    // ============================================================
    internal sealed class MeowAndroidGattServer
    {
        private readonly BluetoothManager _btManager;
        private BluetoothGattServer? _gattServer;
        private readonly HashSet<BluetoothDevice> _subscribers = new();

        private readonly UUID _chatServiceUuid;
        private readonly UUID _msgSendUuid;
        private readonly UUID _msgRecvUuid;

        public event Action<byte[]>? MessageReceived;

        // Brug denne ctor til standard ChatUuids
        public MeowAndroidGattServer(Context ctx)
        {
            _btManager = (BluetoothManager)ctx.GetSystemService(Context.BluetoothService)!;
            _chatServiceUuid = UUID.FromString(ChatUuids.ChatService.ToString())!;
            _msgSendUuid     = UUID.FromString(ChatUuids.MessageSendCharacteristic.ToString())!;
            _msgRecvUuid     = UUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString())!;
        }

        // Overload hvis du vil hoste med custom serviceUuid
        public MeowAndroidGattServer(Context ctx, Guid serviceUuid)
        {
            _btManager = (BluetoothManager)ctx.GetSystemService(Context.BluetoothService)!;
            _chatServiceUuid = UUID.FromString(serviceUuid.ToString())!;
            _msgSendUuid     = UUID.FromString(ChatUuids.MessageSendCharacteristic.ToString())!;
            _msgRecvUuid     = UUID.FromString(ChatUuids.MessageReceiveCharacteristic.ToString())!;
        }

        public void Start()
        {
            _gattServer = _btManager.OpenGattServer(Android.App.Application.Context, new ServerCb(this));

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
        }

        public void Stop()
        {
            try { _gattServer?.Close(); } finally { _subscribers.Clear(); }
        }

        public bool NotifyAll(byte[] data)
        {
            var svc = _gattServer?.GetService(_chatServiceUuid);
            var ch = svc?.GetCharacteristic(_msgSendUuid);
            if (ch == null || _gattServer == null) return false;

            // (Obsolete warning på API 33+ er ok – vi bruger standardvej)
            ch.SetValue(data);
            var ok = false;
            foreach (var dev in _subscribers)
            {
                ok |= _gattServer.NotifyCharacteristicChanged(dev, ch, false);
            }
            return ok;
        }

        private sealed class ServerCb : BluetoothGattServerCallback
        {
            private readonly MeowAndroidGattServer _o;
            public ServerCb(MeowAndroidGattServer o) => _o = o;

            public override void OnConnectionStateChange(BluetoothDevice device, ProfileState status, ProfileState newState)
            {
                if (newState == ProfileState.Connected) _o._subscribers.Add(device);
                else _o._subscribers.Remove(device);
            }

            public override void OnCharacteristicReadRequest(BluetoothDevice device, int requestId, int offset, BluetoothGattCharacteristic characteristic)
            {
                var bytes = characteristic.GetValue() ?? Array.Empty<byte>();
                _o._gattServer?.SendResponse(device, requestId, GattStatus.Success, offset, bytes);
            }

            public override void OnCharacteristicWriteRequest(
                BluetoothDevice device, int requestId, BluetoothGattCharacteristic characteristic,
                bool preparedWrite, bool responseNeeded, int offset, byte[]? value)
            {
                if (characteristic.Uuid.Equals(_o._msgRecvUuid) && value != null)
                    _o.MessageReceived?.Invoke(value);

                var resp = value ?? Array.Empty<byte>();
                if (responseNeeded)
                    _o._gattServer?.SendResponse(device, requestId, GattStatus.Success, offset, resp);
            }
        }
    }
}
#endif
