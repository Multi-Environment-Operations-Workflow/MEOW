#if ANDROID
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using MEOW.Components.Enums;
using MEOW.Components.Models;
using Android.Bluetooth.LE;
using Android.Bluetooth;

namespace MEOW.Components.Services
{
    public class AndroidBluetoothService : IBluetoothService
    {
        private readonly IBluetoothLE _bluetooth = CrossBluetoothLE.Current;
        private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;

        public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
        public ObservableCollection<MeowDevice> Devices { get; } = new();

        private BluetoothLeAdvertiser? _bleAdvertiser;
        private AdvertisingCallback? _advertisingCallback;

        public async Task<bool> ScanAsync()
        {
            if (!await CheckPermissions())
                return false;

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
                    if (a.Device.Name != null && a.Device.Name.StartsWith("(MEOW) "))
                    {
                        var device = new MeowDevice(a.Device.Name, a.Device.Id);
                        device.Name = device.Name.Replace("(MEOW) ", "").Trim();
                        Devices.Add(device);
                    }
                }
            };
            await _adapter.StartScanningForDevicesAsync();
            return true;
            throw new NotImplementedException();
        }

        public Task ConnectAsync(object device)
        {
            throw new NotImplementedException();
        }

        public async Task StartAdvertisingAsync(string name, Guid serviceUuid)
        {
            if (!await CheckPermissions())
                return;

            _bleAdvertiser = BluetoothAdapter.DefaultAdapter?.BluetoothLeAdvertiser;

            if (_bleAdvertiser == null)
            {
                AdvertisingStateChanged?.Invoke(AdvertisingState.Started, "Bluetooth not powered on or not supported.");
                return;
            }
            
            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.Balanced)
                .SetConnectable(true)
                .SetTimeout(0) // 0 = no timeout
                .SetTxPowerLevel(AdvertiseTx.PowerMedium)
                .Build();

            var data = new AdvertiseData.Builder()
                .SetIncludeDeviceName(false)
                .AddServiceUuid(Android.OS.ParcelUuid.FromString(serviceUuid.ToString()))
                .Build();

            _advertisingCallback = new AdvertisingCallback(
                onSuccess: () => AdvertisingStateChanged?.Invoke(AdvertisingState.Started, $"Advertising as {name}"),
                onFailure: (errorMessage) => AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, errorMessage));

            _bleAdvertiser.StartAdvertising(settings, data, _advertisingCallback);
        }

        public Task StopAdvertisingAsync()
        {
            try
            {
                _bleAdvertiser?.StopAdvertising(_advertisingCallback);
                AdvertisingStateChanged?.Invoke(AdvertisingState.Stopped, "Advertising stopped");
            }
            catch (Exception ex)
            {
                AdvertisingStateChanged?.Invoke(AdvertisingState.Failed, $"Failed to stop advertising: {ex.Message}");
            }

            _advertisingCallback = null;

            return Task.CompletedTask;
        }

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
                        await Application.Current.MainPage.DisplayAlert(
                            "Bluetooth Access Required",
                            "This app needs access to bluetooth to function",
                            "OK"
                        );
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

        public AdvertisingCallback(Action onSuccess, Action<String> onFailure)
        {
            _onSuccess = onSuccess;
            _onFailure = onFailure;
        }

        public override void OnStartSuccess(AdvertiseSettings settingsInEffect) => _onSuccess();
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
}

#endif
