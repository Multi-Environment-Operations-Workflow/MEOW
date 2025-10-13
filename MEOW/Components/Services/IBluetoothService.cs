namespace MEOW.Components.Services;

using System.Collections.ObjectModel;
using System.Threading.Tasks;

public interface IBluetoothService
{
    ObservableCollection<object> Devices { get; }
    Task<bool> ScanAsync();
    Task ConnectAsync(object device);
}

