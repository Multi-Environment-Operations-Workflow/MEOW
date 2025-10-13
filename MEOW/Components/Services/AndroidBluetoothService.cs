#if ANDROID
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Content;

namespace MEOW.Components.Services
{
    public class AndroidBluetoothService(Context context) : IBluetoothService
    {
        public ObservableCollection<object> Devices { get; } = new();

        private readonly BluetoothAdapter _adapter = BluetoothAdapter.DefaultAdapter;
        private BluetoothReceiver? _receiver;

        public async Task<bool> ScanAsync()
        {
            Devices.Clear();

            if (!_adapter.IsEnabled)
                throw new Exception("Bluetooth is not enabled.");

            // Add paired (bonded) devices
            foreach (var device in _adapter.BondedDevices)
                Devices.Add(device);

            // Start discovery for unpaired devices
            _receiver ??= new BluetoothReceiver(Devices);
            var filter = new IntentFilter(BluetoothDevice.ActionFound);
            filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);
            context.RegisterReceiver(_receiver, filter);

            bool started = _adapter.StartDiscovery();

            // Wait asynchronously until discovery finishes
            await Task.Run(async () =>
            {
                while (_adapter.IsDiscovering)
                    await Task.Delay(500);
            });

            context.UnregisterReceiver(_receiver);
            return started;
        }

        public async Task ConnectAsync(object device)
        {
            if (device is not BluetoothDevice btDevice)
                throw new ArgumentException("Device is not a BluetoothDevice.", nameof(device));

            try
            {
                _adapter.CancelDiscovery();

                var uuid = btDevice.GetUuids()?.FirstOrDefault()?.Uuid
                           ?? Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB"); // SPP UUID

                using var socket = btDevice.CreateRfcommSocketToServiceRecord(uuid);
                await socket.ConnectAsync();

                // TODO: Store and manage socket for later communication
                Console.WriteLine($"Connected to {btDevice.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
                throw;
            }
        }

        private class BluetoothReceiver : BroadcastReceiver
        {
            private readonly ObservableCollection<object> _devices;

            public BluetoothReceiver(ObservableCollection<object> devices)
            {
                _devices = devices;
            }

            public override void OnReceive(Context? context, Intent? intent)
            {
                if (intent == null)
                    return;

                string? action = intent.Action;
                if (action == BluetoothDevice.ActionFound)
                {
                    var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice)!;
                    // Avoid duplicates by comparing addresses
                    if (!_devices.OfType<BluetoothDevice>().Any(d => d.Address == device.Address))
                        _devices.Add(device);
                }
            }
        }
    }
}
#endif