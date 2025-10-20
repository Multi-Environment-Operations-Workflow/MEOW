using MEOW.Components.Enums;
using MEOW.Components.Models;

namespace MEOW.Components.Services;

using System.Collections.ObjectModel;
using System.Threading.Tasks;

public interface IBluetoothService
{
    public event Action<AdvertisingState, string?>? AdvertisingStateChanged;
    ObservableCollection<MeowDevice> Devices { get; }
    Task<bool> ScanAsync();
    Task ConnectAsync(MeowDevice device);
    
    Task StartAdvertisingAsync(string name, Guid serviceUuid);
    Task StopAdvertisingAsync();
}

