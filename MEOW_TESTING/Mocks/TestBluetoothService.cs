using MEOW_BUSINESS.Enums;
using MEOW_BUSINESS.Services;

namespace MEOW_TESTING.Mocks;

public class TestBluetoothService(IErrorService errorService, ILoggingService loggingService): AbstractBluetoothService(errorService, loggingService), IBluetoothService
{
    public new event Action<AdvertisingState, string?>? AdvertisingStateChanged;
    public new event Action? PeerConnected;
    public new event Action<byte[]>? DeviceDataReceived;

    public Task StartAdvertisingAsync(string name)
    {
        throw new NotImplementedException();
    }

    public Task StopAdvertisingAsync()
    {
        throw new NotImplementedException();
    }
}