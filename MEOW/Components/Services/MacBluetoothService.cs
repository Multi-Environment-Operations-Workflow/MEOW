using System.Collections.ObjectModel;
using MEOW.Components.Enums;

#if MACCATALYST
namespace MEOW.Components.Services;

public class MacBluetoothService: IBluetoothService
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

#endif