using MEOW.Components.Enums;

namespace MEOW.Components.Services;

using System.Collections.ObjectModel;
using System.Threading.Tasks;

public interface IBluetoothService
{
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
    ObservableCollection<object> Devices { get; }
    Task<bool> ScanAsync();
    Task ConnectAsync(object device);
    
    Task StartAdvertisingAsync(string name, Guid serviceUuid);
    Task StopAdvertisingAsync();
}

