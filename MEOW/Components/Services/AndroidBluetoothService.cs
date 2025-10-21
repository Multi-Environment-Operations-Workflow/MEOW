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

        /// <summary>
        /// Scans the surrounding area for Bluetooth devices.
        /// Only devices with names starting with "(MEOW) " are added to the Devices collection
        /// </summary>
        /// <returns>True if the scan was finished successfully.</returns>
        /// <exception cref="InvalidOperationException">If Bluetooth is not initialized.</exception>
        /// <exception cref="Exception">If Bluetooth is off.</exception>
        /// <exception cref="PermissionException">If Bluetooth permission is denied.</exception>
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
                    if (a.Device.Name != null && a.Device.Name.StartsWith("(MEOW) "))
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

        public Task ConnectAsync(MeowDevice device)
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

        /// <summary>
        /// Checks and requests Bluetooth permissions on Android.
        /// </summary>
        /// <returns>bool indicating if permission is granted.</returns>
        /// <exception cref="PermissionException">If permission is denied.</exception>
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
}
#endif
