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
    
    
    public class AndroidBluetoothService : IBluetoothService // Implementering af IBluetoothService til Android
    {

        // Plugin abstraktion til Bluetooth local enhed. Giver os adgang til fx _bluetooth.IsOn
        // Tjekker, om Bluetooth er slået til, tilladelser, tilgængelighed osv.
        private readonly IBluetoothLE _bluetooth = CrossBluetoothLE.Current;

        // instance af Bluetooth-adapteren, som håndterer scanning, forbindelser osv.
        private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
        public event Action? PeerConnected;
        public event Action<byte[]>? DeviceDataReceived;

        // Enum vi bruger til at holde styr på advertising state. 
        // AdvertisingStateChanged eventet bruges til at informere UI'et om ændringer i advertising status.
        // Så vi fx på "front enden" kan sætte funktioner til at kører via dette event. Man kan se det som en tom funktion.
        // Hvor man kan tilføje funktioner der skal kører når eventet bliver "udløst".
        // Action<AdvertisingState, string?> betyder at funktionerne der tilføjes til eventet skal tage to parametre: kan ikke tilføjes vis ikke vi har dem.
        public event Action<AdvertisingState, string?>? AdvertisingStateChanged;

        //Til at informere UI'et om, at en peer er forbundet, uden information på
        public event Action? PeerConnected;

        // Udløst når vi modtager data fra en forbundet enhed.
        public event Action<byte[]>? DeviceDataReceived;


        // Liste vi bruger til at holde styr på fundne enheder under scanning.
        // Devices.Add(device);  også til ui (var d in _devices) <div> d.Name</div>
        public ObservableCollection<MeowDevice> Devices { get; } = new();


        //Styre advertising (Bluetooth peripheral) “Server” Gør sig synlig, venter på at nogen forbinder
        //En funktion du tænder og slukker for (som “gør mig synlig for andre”) Android specific, plugin gør det ikke
        private BluetoothLeAdvertiser? _bleAdvertiser;

        // Android specific callback til at håndtere advertising resultater
        // Som vi så sender videre via AdvertisingStateChanged eventet
        private AdvertisingCallback? _advertisingCallback;

        // Lokal GATT-server så Android kan modtage beskeder. Er "client" i forhold til peripheral server forholdet. 
        private MeowAndroidGattServer? _gattServer;

        public int GetConnectedDevicesCount()
        {
            return _adapter.ConnectedDevices?.Count ?? 0;
        }

        
        public async Task<(bool, List<Exception>)> SendToAllAsync(byte[] data)
        {
            var anySuccess = false;
            var exceptions = new List<Exception>();

            // Send som CENTRAL/server/host til allerede forbundne enheder (Plugin.BLE)
            foreach (var device in _adapter.ConnectedDevices.ToList())
            {
                try
                {
                    // Standard måde at sende pakker er en størelse af 20 bytes pr pakke. Men det er ikke effektivt. 
                    // Så vi forspørger om vi kan få lov til at sende en større MTU pakke (max 185 bytes)
                    // Gør det hurtigere. Aner ikke hvor meget hurtige dog....
                    try { await device.RequestMtuAsync(185).ConfigureAwait(false); } catch { /* best effort */ }


                    // Services -> Chatservervice -> Characteristics -> messagereciveChar.
                    // Er Services på anden device, finder chatservice, finder hvad vi kan gøre på chatservice, finder characteristic vi kan skrive til.
                    // Finder services, som den anden enhed "tilbyder" Burde gerne være de samme, i denne fil.
                    // Vi får Generic Attribute Profile (Gatt). Nødvendigt da IOS kun kan komunikere via GATT.
                    var services = await device.GetServicesAsync().ConfigureAwait(false);
                    var chatService = services.FirstOrDefault(s => s.Id == ChatUuids.ChatService);
                    if (chatService == null)
                        throw new Exception($"Service {ChatUuids.ChatService} not found on {device.Name}");
                    var characteristics = await chatService.GetCharacteristicsAsync().ConfigureAwait(false);
                    var messageReceiveChar = characteristics.FirstOrDefault(c => c.Id == ChatUuids.MessageReceiveCharacteristic);
                    if (messageReceiveChar == null)
                        throw new Exception($"Characteristic {ChatUuids.MessageReceiveCharacteristic} not found on {device.Name}");

                    // Plugin.BLE: brug WriteType + WriteAsync(data)
                    // Her er det så at vi faktisk sender data mellem enhederne. WithResponse = bekræftelse på modtagelse.
                    // Vi sender som Central til folk på denne måde.  Og modtager beskeder på denne måde i peripheral ish. 
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
            // Som Peripheral sender vi data via denne, og som central modtager vi data via denne.
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
            // Start håndtering af GATT-requests
            _gattServer.Start();

            // 2) Advertising med service UUID = ChatUuids.ChatService
            // Mere opsætning til advertising
            var bluetoothManager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
            var adapter = bluetoothManager?.Adapter;
            var advertiser = adapter?.BluetoothLeAdvertiser;

            if (advertiser == null)
            {
                AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, "Bluetooth not powered on or not supported.");
                return;
            }

            // Det navn folk vil se i deres Bluetooth-liste
            adapter?.SetName($"(MEOW) {name}");
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

        // Overload med custom serviceUuid — samme logik, bare bruger den UUID
        public async Task StartAdvertisingAsync(string name, Guid serviceUuid)
        {
            if (!await CheckPermissions())
                return;

            // 1) GATT-server (serviceUuid bruges her)
            //Gemmer UUID’er for: selve chat-servicen, “send besked”-characteristikken, “modtag besked”-characteristikken.
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

            Devices.Clear(); // Vi fjerner alle tidigere devices før en ny scan. !! Kan give problemer måske
            //betyder denne logik ikke at vi Skal connecte til alle devices før vi hopper i en chat, for ellers skal vi connecte til alle igen for at tilføje et nyt medlem?
            if (_bluetooth == null || _adapter == null)
                throw new InvalidOperationException("Bluetooth not initialized");

            if (!_bluetooth.IsOn)
                throw new Exception("Bluetooth is off");

            var foundDevices = new List<IDevice>();
            _adapter.DeviceDiscovered += (s, a) => // Event der sker hver gang en enhed bliver fundet under scanning
            {
                if (!foundDevices.Contains(a.Device))
                {
                    foundDevices.Add(a.Device);
                    if (a.Device.Name != null)
                    {// Vi fjerner (MEOW) fra navnet, ser pænere ud i UI'et. Det er så genialt at det bare kommer med alt. 
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

        
        public int GetConnectedDevicesCount()
        {
            throw new NotImplementedException();
        }

        public Task<(bool anySuccess, List<Exception> allErrors)> SendToAllAsync(byte[] data)
        {
            throw new NotImplementedException();
        }

        public Task StartAdvertisingAsync(string name)
        {
            throw new NotImplementedException();
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
