using MEOW_BUSINESS.Services;
using MEOW_TESTING.Mocks;
using NSubstitute;

namespace MEOW_TESTING;

public class AbstractBluetoothServiceTest
{
    [Fact]
    public async Task ScanForDevices_Works()
    {
        var errorService = new TestErrorService();
        var loggingService = new LoggingService();
        var abstractBluetoothService = new TestBluetoothService(errorService, loggingService);
        await abstractBluetoothService.ScanForDevices();
        
    }
}