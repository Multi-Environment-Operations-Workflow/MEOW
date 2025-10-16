#if ANDROID
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using MEOW.Components.Enums;
using MEOW.Components.Models;

namespace MEOW.Components.Services
{
    public class AndroidBluetoothService : IBluetoothService
    {
        private readonly IBluetoothLE _bluetooth = CrossBluetoothLE.Current;
        private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;

        public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
        public ObservableCollection<MeowDevice> Devices { get; } = new();
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

        public Task StartAdvertisingAsync(string name, Guid serviceUuid)
        {
            throw new NotImplementedException();
        }

        public Task StopAdvertisingAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> CheckPermissions() 
        {
            try
            {
                PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();

                if (status == PermissionStatus.Granted)
                    return true;

                if (status == PermissionStatus.Denied)
                {
                    if (Permissions.ShouldShowRationale<Permissions.Bluetooth>())
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Bluetooth Access Required",
                            "This app needs access to bluetooth to function",
                            "OK"
                        );
                    }

                    status = await Permissions.RequestAsync<Permissions.Bluetooth>();
                    return status == PermissionStatus.Granted;
                }
                status = await Permissions.RequestAsync<Permissions.Bluetooth>();
                return status == PermissionStatus.Granted;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Permission check failed: {ex.Message}");
                throw;
            }
        }
    }
}
#endif
