#if ANDROID
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Content;
using Android.Bluetooth.LE;
using Java.Util;


namespace MEOW.Components.Services
{
    public class AndroidBluetoothService(Context context) : IBluetoothService
    {
        public ObservableCollection<object> Devices { get; }
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