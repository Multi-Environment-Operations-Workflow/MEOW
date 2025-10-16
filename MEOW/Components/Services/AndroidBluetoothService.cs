#if ANDROID
using System.Collections.ObjectModel;
using MEOW.Components.Enums;

namespace MEOW.Components.Services
{
    public class AndroidBluetoothService : IBluetoothService
    {
        public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
        public ObservableCollection<object> Devices { get; } = new();
        public Task<bool> ScanAsync()
        {
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
    }
}
#endif